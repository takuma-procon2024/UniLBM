using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Common;
using Solver;
using Unity.Mathematics;
using UnityEngine;

namespace Cloth
{
    public class ClothLbmIntegrator : MonoBehaviour
    {
        [Header("Parameters")] [SerializeField]
        private float cellSize = 1f;

        [SerializeField] private Vector3 offset;

        [Header("Resources")] [SerializeField] private LbmSolverBehaviour lbmSolver;

        [SerializeField] private ClothSimulationBehaviour clothSimulation;
        [SerializeField] private ComputeShader computeShader;

        private bool _isInitialized;

        private ComputeShaderWrapper<Kernels, Uniforms> _shaderWrapper;

        private IEnumerator Start()
        {
            _isInitialized = false;
            yield return new WaitUntil(() => lbmSolver?.Solver?.GetFieldBuffer() != null);

            _shaderWrapper = new ComputeShaderWrapper<Kernels, Uniforms>(computeShader);

            InitBuffers();
            CalcDispatchGroups();

            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized) return;
            var pos = transform.position + offset;
            var scale = transform.localScale;
            var rotation = transform.rotation;
            var transformMatrix = Matrix4x4.TRS(pos, rotation, scale);
            computeShader.SetMatrix(_shaderWrapper.UniformMap[Uniforms.transform], transformMatrix);

            computeShader.SetFloat(_shaderWrapper.UniformMap[Uniforms.lbm_cell_size], cellSize);

            computeShader.Dispatch(_shaderWrapper.KernelMap[Kernels.reset_field], (int)_resetKernelGroups.x,
                (int)_resetKernelGroups.y, (int)_resetKernelGroups.z);
            computeShader.Dispatch(_shaderWrapper.KernelMap[Kernels.main], (int)_mainKernelGroups.x,
                (int)_mainKernelGroups.y, 1);
        }

        #region ComputeShdaer

        private uint3 _resetKernelGroups;
        private uint2 _mainKernelGroups;

        private void CalcDispatchGroups()
        {
            computeShader.GetKernelThreadGroupSizes(_shaderWrapper.KernelMap[Kernels.reset_field],
                out var resetX, out var resetY, out var resetZ);
            computeShader.GetKernelThreadGroupSizes(_shaderWrapper.KernelMap[Kernels.main],
                out var mainX, out var mainY, out _);

            var lbmCellRes = lbmSolver.Solver.GetCellSize();
            var clothRes = clothSimulation.ClothResolution;
            _resetKernelGroups = new uint3(
                (uint)math.ceil(lbmCellRes / (float)resetX),
                (uint)math.ceil(lbmCellRes / (float)resetY),
                (uint)math.ceil(lbmCellRes / (float)resetZ)
            );
            _mainKernelGroups = new uint2(
                (uint)math.ceil((clothRes.x - 1f) / mainX),
                (uint)math.ceil((clothRes.y - 1f) / mainY)
            );
        }

        private void InitBuffers()
        {
            computeShader.SetBuffer(_shaderWrapper.KernelMap[Kernels.reset_field],
                _shaderWrapper.UniformMap[Uniforms.field_buffer],
                lbmSolver.Solver.GetFieldBuffer());
            computeShader.SetBuffer(_shaderWrapper.KernelMap[Kernels.main],
                _shaderWrapper.UniformMap[Uniforms.field_buffer],
                lbmSolver.Solver.GetFieldBuffer());
            computeShader.SetTexture(_shaderWrapper.KernelMap[Kernels.main],
                _shaderWrapper.UniformMap[Uniforms.pos_buffer],
                clothSimulation.PositionBuffer);

            computeShader.SetInt(_shaderWrapper.UniformMap[Uniforms.lbm_res], (int)lbmSolver.Solver.GetCellSize());
            computeShader.SetInts(_shaderWrapper.UniformMap[Uniforms.cloth_res], (int)clothSimulation.ClothResolution.x,
                (int)clothSimulation.ClothResolution.y);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Kernels
        {
            reset_field,
            main
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Uniforms
        {
            pos_buffer,
            field_buffer,
            cloth_res,
            lbm_res,
            lbm_cell_size,
            transform
        }

        #endregion
    }
}