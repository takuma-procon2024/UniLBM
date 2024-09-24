using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Lbm;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace UniIntegrator
{
    /// <summary>
    ///     ファンの風の大きさをLBMソルバーに伝えるためのコンポーネント
    /// </summary>
    public class LbmFanIntegrator : MonoBehaviour
    {
        [Header("Properties")] [Header("配置するファンの数")] [SerializeField]
        private uint2 fanNum;

        [Header("ファンが風を出すセルの大きさ")] [SerializeField]
        private uint3 fanSize;

        [Header("ファンの向き")] [SerializeField] private float3 fanDir;

        [Header("ファン同士の間隔 セルの大きさ")] [SerializeField]
        private uint2 fanMargin;

        [Header("ファンを配置するオフセット")] [SerializeField]
        private float3 fanOffset;

        [Header("Resources")] [Header("コンピュートシェーダー")] [SerializeField]
        private ComputeShader computeShader;

        [Header("LBMソルバー")] [SerializeField] private LbmSolverBehaviour lbmSolver;

#if UNITY_EDITOR
        [Header("Debugs")] [SerializeField] private float[] debugPowers;
#endif
        private bool _isInitialized;

        private float[] fanPowers;

        /// <summary>
        ///     インスペクタから設定されたファンの数
        /// </summary>
        public uint2 FanNum => fanNum;

        private IEnumerator Start()
        {
            Assert.IsNotNull(lbmSolver);
            yield return new WaitUntil(() => lbmSolver.IsInitialized);

            InitBuffers();
            InitShader();

            _isInitialized = true;

#if UNITY_EDITOR
            debugPowers = new float[fanNum.x * fanNum.y];
#endif
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!_isInitialized) return;

            TrySetFanPowers(id => debugPowers[id.y * fanNum.x + id.x]);
        }
#endif

        /// <summary>
        ///     ファンパワーを設定する
        /// </summary>
        /// <param name="getPower">引数として渡されたIDに基づくファンの強さを返す関数</param>
        /// <returns>ファンパワーの設定が成功したか</returns>
        public bool TrySetFanPowers(in Func<uint2, float> getPower)
        {
            if (!_isInitialized) return false;

            for (var y = 0; y < fanNum.y; y++)
            for (var x = 0; x < fanNum.x; x++)
                fanPowers[y * fanNum.x + x] = getPower(new uint2((uint)x, (uint)y));

            fanPowerBuffer.SetData(fanPowers);
            computeShader.Dispatch(kernelMap[Kernels.set_fan_power], _threadGroups.x, _threadGroups.y, 1);

            return true;
        }

        #region ComputeShader

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Kernels
        {
            set_fan_power
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Uniforms
        {
            lbm_force_buffer,
            fan_power_buffer,
            fan_dir,
            fan_margin,
            fan_offset,
            fan_size,
            lbm_res,
            fan_num
        }

        private Dictionary<Kernels, int> kernelMap;
        private Dictionary<Uniforms, int> uniformMap;
        private int2 _threadGroups;

        private GraphicsBuffer fanPowerBuffer;

        private void InitBuffers()
        {
            fanPowers = new float[fanNum.x * fanNum.y];
            fanPowerBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, fanPowers.Length, sizeof(float));
        }

        private void InitShader()
        {
            kernelMap = Enum.GetValues(typeof(Kernels)).Cast<Kernels>()
                .ToDictionary(t => t, t => computeShader.FindKernel(t.ToString()));
            uniformMap = Enum.GetValues(typeof(Uniforms)).Cast<Uniforms>()
                .ToDictionary(t => t, t => Shader.PropertyToID(t.ToString()));

            computeShader.GetKernelThreadGroupSizes(kernelMap[Kernels.set_fan_power], out var x, out var y, out _);
            _threadGroups = new int2(math.ceil(fanNum / new float2(x, y)));

            SetConstantsToShader();
            computeShader.SetBuffer(kernelMap[Kernels.set_fan_power], uniformMap[Uniforms.lbm_force_buffer],
                lbmSolver.Solver.GetExternalForceBuffer());
            computeShader.SetBuffer(kernelMap[Kernels.set_fan_power], uniformMap[Uniforms.fan_power_buffer],
                fanPowerBuffer);
        }

        private void SetConstantsToShader()
        {
            computeShader.SetFloats(uniformMap[Uniforms.fan_dir], fanDir.x, fanDir.y, fanDir.z);
            computeShader.SetVector(uniformMap[Uniforms.fan_margin], new Vector2(fanMargin.x, fanMargin.y));
            computeShader.SetFloats(uniformMap[Uniforms.fan_offset], fanOffset.x, fanOffset.y, fanOffset.z);
            computeShader.SetVector(uniformMap[Uniforms.fan_size], new Vector3(fanSize.x, fanSize.y, fanSize.z));
            computeShader.SetInt(uniformMap[Uniforms.lbm_res], (int)lbmSolver.Solver.GetCellSize());
            computeShader.SetVector(uniformMap[Uniforms.fan_num], new Vector2(fanNum.x, fanNum.y));
        }

        #endregion
    }
}