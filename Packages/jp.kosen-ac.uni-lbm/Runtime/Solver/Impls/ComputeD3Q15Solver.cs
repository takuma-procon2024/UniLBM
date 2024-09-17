using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Mathematics;
using UnityEngine;

namespace Solver.Impls
{
    public class ComputeD3Q15Solver : UniLbmSolverBase
    {
        private readonly uint _cellSize;
        private readonly float3 _force;
        private readonly Dictionary<Kernels, int> _kernelMap;
        private readonly float _tau;
        private readonly Dictionary<Uniforms, int> _uniformMap;

        public ComputeD3Q15Solver(ComputeShader computeShader, uint cellSize, float tau, float3 force)
            : base(computeShader)
        {
            _cellSize = cellSize;
            _tau = tau;
            _force = force;

            InitializeSolverBase(out _kernelMap, out _uniformMap);
            InitializeBuffers();
            SetupShader();
        }

        public override void Dispose()
        {
            base.Dispose();

            _f0Buffer?.Dispose();
            _f1Buffer?.Dispose();
            _fieldBuffer?.Dispose();
            _forceSourceBuffer?.Dispose();
            _velocityBuffer?.Dispose();
        }

        public override void Step()
        {
            CalcDispatchThreadGroups(out var groupX, out var groupY, out var groupZ, _cellSize);
            ComputeShader.Dispatch(_kernelMap[Kernels.solve], groupX, groupY, groupZ);
            ComputeShader.Dispatch(_kernelMap[Kernels.swap], groupX, groupY, groupZ);
        }

        public override GraphicsBuffer GetFieldBuffer()
        {
            return _fieldBuffer;
        }

        public override GraphicsBuffer GetVelocityBuffer()
        {
            return _velocityBuffer;
        }

        #region Initialize

        private void InitializeBuffers()
        {
            var totalSize = (int)(_cellSize * _cellSize * _cellSize);
            _f0Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalSize * Q, sizeof(float));
            _f1Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalSize * Q, sizeof(float));
            _fieldBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalSize, sizeof(uint));
            _forceSourceBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalSize, 3 * sizeof(float));
            _velocityBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalSize, 3 * sizeof(float));
        }

        private void SetupShader()
        {
            ComputeShader.SetBuffer(_kernelMap[Kernels.solve], _uniformMap[Uniforms.f0], _f0Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.solve], _uniformMap[Uniforms.f1], _f1Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.solve], _uniformMap[Uniforms.field], _fieldBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.solve], _uniformMap[Uniforms.force_source], _forceSourceBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.solve], _uniformMap[Uniforms.velocity], _velocityBuffer);

            ComputeShader.SetBuffer(_kernelMap[Kernels.initialize], _uniformMap[Uniforms.f0], _f0Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.initialize], _uniformMap[Uniforms.field], _fieldBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.initialize], _uniformMap[Uniforms.force_source],
                _forceSourceBuffer);

            ComputeShader.SetBuffer(_kernelMap[Kernels.swap], _uniformMap[Uniforms.f0], _f0Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.swap], _uniformMap[Uniforms.f1], _f1Buffer);

            ComputeShader.SetInt(_uniformMap[Uniforms.cell_size], (int)_cellSize);
            ComputeShader.SetVector(_uniformMap[Uniforms.force], new Vector4(_force.x, _force.y, _force.z));
            ComputeShader.SetFloat(_uniformMap[Uniforms.tau], _tau);

            // Dispatch initialization kernel
            CalcDispatchThreadGroups(out var groupX, out var groupY, out var groupZ, _cellSize);
            ComputeShader.Dispatch(_kernelMap[Kernels.initialize], groupX, groupY, groupZ);
        }

        #endregion

        #region ComputeShader properties

        private const int Q = 15;

        private GraphicsBuffer _f0Buffer, _f1Buffer, _fieldBuffer, _forceSourceBuffer, _velocityBuffer;


        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Uniforms
        {
            f0,
            f1,
            field,
            force_source,
            velocity,
            cell_size,
            force,
            tau
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Kernels
        {
            solve,
            initialize,
            swap
        }

        #endregion
    }
}