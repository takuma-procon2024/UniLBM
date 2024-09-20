using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Solver;
using Unity.Mathematics;
using UnityEngine;

namespace Cloth
{
    public class ClothSimulationBehaviour : MonoBehaviour
    {
        [Header("Simulation Parameters")] [SerializeField]
        private float timeStep = 0.01f;

        [Range(1, 16)] [SerializeField] private int verletIteration = 4;

        [SerializeField] private uint2 clothResolution = new(128, 128);
        [SerializeField] private float restLength = 0.02f;
        [SerializeField] private float stiffness = 10000f;
        [SerializeField] private float damping = 0.996f;
        [SerializeField] private float mass = 1.0f;
        [SerializeField] private float3 gravity = new(0, -9.81f, 0);
        [SerializeField] private float lbmCellSize = 1;
        [SerializeField] private float lbmForceScale = 1;

        [Header("References")] [SerializeField]
        private LbmSolverBehaviour lbmSolver;

        [Header("Resources")] [SerializeField] private ComputeShader computeShader;

        public uint2 ClothResolution => clothResolution;
        private bool _isInitialized;

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => lbmSolver?.Solver?.GetVelocityBuffer() != null);
            _isInitialized = true;
            
            InitComputeShader();
            InitBuffers();
            ResetBuffer();

            if (TryGetComponent(out ClothRenderer clothRenderer))
                clothRenderer.InitializeClothRenderer(this);
        }

        private void Update()
        {
            if (!_isInitialized) return;
            Simulation();
        }

        private void OnDestroy()
        {
            ReleaseBuffers();
        }

        private void OnGUI()
        {
            DrawSimulationBufferOnGui();
        }

        #region Debug

        [Header("Debug")] [SerializeField] private bool drawSimulationBuffer;

        [SerializeField] private float debugDrawScale;

        [Conditional("UNITY_EDITOR")]
        private void DrawSimulationBufferOnGui()
        {
            if (!drawSimulationBuffer) return;

            var rw = (int)math.round(clothResolution.x * debugDrawScale);
            var rh = (int)math.round(clothResolution.y * debugDrawScale);

            var storeColor = GUI.color;
            GUI.color = Color.gray;

            var r00 = new Rect(rw * 0, rh * 0, rw, rh);
            var r01 = new Rect(rw * 1, rh * 0, rw, rh);
            var r10 = new Rect(rw * 0, rh * 1, rw, rh);
            var r11 = new Rect(rw * 1, rh * 1, rw, rh);
            var r20 = new Rect(rw * 0, rh * 2, rw, rh);

            GUI.DrawTexture(r00, _positionBuffer[0]);
            GUI.DrawTexture(r01, _positionBuffer[1]);
            GUI.DrawTexture(r10, _prevPosBuffer[0]);
            GUI.DrawTexture(r11, _prevPosBuffer[1]);
            GUI.DrawTexture(r20, _normalBuffer);

            GUI.Label(r00, "Position Buffer 0");
            GUI.Label(r01, "Position Buffer 1");
            GUI.Label(r10, "Prev Position Buffer 0");
            GUI.Label(r11, "Prev Position Buffer 1");
            GUI.Label(r20, "Normal Buffer");

            GUI.color = storeColor;
        }

        #endregion

        #region Utils

        private static RenderTexture CreateRenderTexture(in int2 res, in RenderTextureFormat format,
            in FilterMode filter)
        {
            var buffer = new RenderTexture(res.x, res.y, 0, format)
            {
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                enableRandomWrite = true
            };
            buffer.Create();
            return buffer;
        }

        private RenderTexture CreateRenderTexture()
        {
            return CreateRenderTexture(new int2(clothResolution), RenderTextureFormat.ARGBFloat, FilterMode.Point);
        }

        private static void ReleaseBuffers(ref RenderTexture rt)
        {
            if (Application.isEditor)
                DestroyImmediate(rt);
            else
                Destroy(rt);
            rt = null;
        }

        private void ReleaseBuffers()
        {
            ReleaseBuffers(ref _normalBuffer);
            ReleaseBuffers(ref _inputForceBuffer);
            ReleaseBuffers(ref _positionBuffer[0]);
            ReleaseBuffers(ref _positionBuffer[1]);
            ReleaseBuffers(ref _prevPosBuffer[0]);
            ReleaseBuffers(ref _prevPosBuffer[1]);
        }

        #endregion

        #region ComputeShader

        private RenderTexture[] _positionBuffer;
        private RenderTexture[] _prevPosBuffer;
        private RenderTexture _normalBuffer;
        private RenderTexture _inputForceBuffer;
        private float2 _totalClothLength;

        private uint2 _groupThreads;
        private Dictionary<Kernels, int> _kernelMap;
        private Dictionary<Uniforms, int> _uniformMap;

        public RenderTexture PositionBuffer => _positionBuffer[0];
        public RenderTexture NormalBuffer => _normalBuffer;
        public RenderTexture InputForceBuffer => _inputForceBuffer;

        private void InitComputeShader()
        {
            _kernelMap = Enum.GetValues(typeof(Kernels))
                .Cast<Kernels>()
                .ToDictionary(k => k, k => computeShader.FindKernel(k.ToString()));
            _uniformMap = Enum.GetValues(typeof(Uniforms))
                .Cast<Uniforms>()
                .ToDictionary(u => u, u => Shader.PropertyToID(u.ToString()));

            computeShader.GetKernelThreadGroupSizes(_kernelMap[Kernels.init], out var x, out var y, out _);
            _groupThreads = new uint2(
                (uint)math.ceil((float)clothResolution.x / x),
                (uint)math.ceil((float)clothResolution.y / y)
            );
        }

        private void InitBuffers()
        {
            _positionBuffer = new RenderTexture[2];
            _prevPosBuffer = new RenderTexture[2];
            for (var i = 0; i < 2; i++)
            {
                _positionBuffer[i] = CreateRenderTexture();
                _prevPosBuffer[i] = CreateRenderTexture();
            }

            _normalBuffer = CreateRenderTexture();
            _inputForceBuffer = CreateRenderTexture();

            SetBuffers();
        }

        private void Simulation()
        {
            var dt = timeStep / verletIteration;
            var trs = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);

            computeShader.SetMatrix(_uniformMap[Uniforms.lbm_transform], trs);
            // PERF: 多分毎フレーム設定する必要ない
            computeShader.SetFloat(_uniformMap[Uniforms.dt], dt);

#if UNITY_EDITOR
            // DEBUG: SerializeFieldからの変更を反映するために毎フレーム設定
            computeShader.SetInt(_uniformMap[Uniforms.lbm_cell_res], (int)lbmSolver.Solver.GetCellSize());
            computeShader.SetFloat(_uniformMap[Uniforms.lbm_cell_size], lbmCellSize);
            computeShader.SetFloat(_uniformMap[Uniforms.lbm_force_scale], lbmForceScale);
#endif

            for (var i = 0; i < verletIteration; i++)
            {
                computeShader.Dispatch(_kernelMap[Kernels.simulation], (int)_groupThreads.x, (int)_groupThreads.y, 1);
                SwapBuffers();
            }
        }

        private void SwapBuffers()
        {
            var tmp = _positionBuffer[0];
            _positionBuffer[0] = _positionBuffer[1];
            _positionBuffer[1] = tmp;

            tmp = _prevPosBuffer[0];
            _prevPosBuffer[0] = _prevPosBuffer[1];
            _prevPosBuffer[1] = tmp;

            var kernel = _kernelMap[Kernels.simulation];
            computeShader.SetTexture(kernel, _uniformMap[Uniforms.pos_prev_buffer], _prevPosBuffer[0]);
            computeShader.SetTexture(kernel, _uniformMap[Uniforms.pos_curr_buffer], _positionBuffer[0]);
            computeShader.SetTexture(kernel, _uniformMap[Uniforms.pos_prev_buffer_out], _prevPosBuffer[1]);
            computeShader.SetTexture(kernel, _uniformMap[Uniforms.pos_curr_buffer_out], _positionBuffer[1]);
        }

        private void SetBuffers()
        {
            _totalClothLength = new float2(
                clothResolution.x * restLength,
                clothResolution.y * restLength
            );

            computeShader.SetInts(_uniformMap[Uniforms.cloth_resolution], (int)clothResolution.x,
                (int)clothResolution.y);
            computeShader.SetFloats(_uniformMap[Uniforms.total_cloth_length], _totalClothLength.x, _totalClothLength.y);
            computeShader.SetFloat(_uniformMap[Uniforms.rest_length], restLength);
            computeShader.SetFloat(_uniformMap[Uniforms.stiffness], stiffness);
            computeShader.SetFloat(_uniformMap[Uniforms.damp], damping);
            computeShader.SetFloat(_uniformMap[Uniforms.inv_mass], 1.0f / mass);
            computeShader.SetFloats(_uniformMap[Uniforms.gravity], gravity.x, gravity.y, gravity.z);

            computeShader.SetInt(_uniformMap[Uniforms.lbm_cell_res], (int)lbmSolver.Solver.GetCellSize());
            computeShader.SetFloat(_uniformMap[Uniforms.lbm_cell_size], lbmCellSize);
            computeShader.SetFloat(_uniformMap[Uniforms.lbm_force_scale], lbmForceScale);

            computeShader.SetTexture(_kernelMap[Kernels.init], _uniformMap[Uniforms.pos_prev_buffer_out],
                _prevPosBuffer[1]);
            computeShader.SetTexture(_kernelMap[Kernels.init], _uniformMap[Uniforms.pos_curr_buffer_out],
                _positionBuffer[1]);
            computeShader.SetTexture(_kernelMap[Kernels.init], _uniformMap[Uniforms.normal_buffer_out], _normalBuffer);

            computeShader.SetTexture(_kernelMap[Kernels.simulation], _uniformMap[Uniforms.pos_prev_buffer],
                _prevPosBuffer[0]);
            computeShader.SetTexture(_kernelMap[Kernels.simulation], _uniformMap[Uniforms.pos_curr_buffer],
                _positionBuffer[0]);
            computeShader.SetTexture(_kernelMap[Kernels.simulation], _uniformMap[Uniforms.pos_prev_buffer_out],
                _prevPosBuffer[1]);
            computeShader.SetTexture(_kernelMap[Kernels.simulation], _uniformMap[Uniforms.pos_curr_buffer_out],
                _positionBuffer[1]);
            computeShader.SetTexture(_kernelMap[Kernels.simulation], _uniformMap[Uniforms.normal_buffer_out],
                _normalBuffer);
            computeShader.SetTexture(_kernelMap[Kernels.simulation], _uniformMap[Uniforms.input_force],
                _inputForceBuffer);
            computeShader.SetBuffer(_kernelMap[Kernels.simulation], _uniformMap[Uniforms.lbm_velocity],
                lbmSolver.Solver.GetVelocityBuffer());
        }

        private void ResetBuffer()
        {
            computeShader.Dispatch(_kernelMap[Kernels.init], (int)_groupThreads.x, (int)_groupThreads.y, 1);
            Graphics.Blit(_positionBuffer[1], _positionBuffer[0]);
            Graphics.Blit(_prevPosBuffer[1], _prevPosBuffer[0]);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Kernels
        {
            init,
            simulation
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Uniforms
        {
            pos_prev_buffer,
            pos_curr_buffer,
            pos_prev_buffer_out,
            pos_curr_buffer_out,
            normal_buffer_out,
            input_force,
            cloth_resolution,
            total_cloth_length,
            rest_length,
            gravity,
            stiffness,
            damp,
            inv_mass,
            dt,

            lbm_velocity,
            lbm_cell_res,
            lbm_cell_size,
            lbm_force_scale,
            lbm_transform
        }

        #endregion
    }
}