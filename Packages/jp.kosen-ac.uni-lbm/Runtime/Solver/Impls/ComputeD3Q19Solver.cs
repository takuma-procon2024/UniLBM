using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Solver.Impls
{
    public class ComputeD3Q19Solver : UniLbmSolverBase
    {
        private readonly uint _cellSize;
        private readonly Dictionary<Kernels, int> _kernelMap;
        private readonly float _tau;
        private readonly Dictionary<Uniforms, int> _uniformMap;

        public ComputeD3Q19Solver(ComputeShader computeShader, uint cellSize, float tau) : base(
            computeShader)
        {
            _cellSize = cellSize;
            _tau = tau;

            InitializeSolverBase(out _kernelMap, out _uniformMap);
            InitializeBuffers();
            InitializeShader();
        }

        private void SwapBuffers()
        {
            (_f0Buffer, _f1Buffer) = (_f1Buffer, _f0Buffer);

            ComputeShader.SetBuffer(_kernelMap[Kernels.collision], _uniformMap[Uniforms.f0], _f0Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.collision], _uniformMap[Uniforms.f1], _f1Buffer);

            ComputeShader.SetBuffer(_kernelMap[Kernels.advection], _uniformMap[Uniforms.f0], _f0Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.advection], _uniformMap[Uniforms.f1], _f1Buffer);
        }

        public override void Step()
        {
            CalcDispatchThreadGroups(out var groupX, out var groupY, out var groupZ, _cellSize);
            ComputeShader.Dispatch(_kernelMap[Kernels.collision], groupX, groupY, groupZ);
            ComputeShader.Dispatch(_kernelMap[Kernels.advection], groupX, groupY, groupZ);

            SwapBuffers();
        }

        public override GraphicsBuffer GetFieldBuffer()
        {
            return _fieldBuffer;
        }

        public override GraphicsBuffer GetVelocityBuffer()
        {
            return _velocityBuffer;
        }

        public override uint GetCellSize()
        {
            return _cellSize;
        }

        public override void Dispose()
        {
            base.Dispose();

            _f0Buffer.Dispose();
            _f1Buffer.Dispose();
            _fieldBuffer.Dispose();
            _forceSourceBuffer.Dispose();
            _velocityBuffer.Dispose();
            _densityBuffer.Dispose();

            _f0Buffer = null;
            _f1Buffer = null;
            _fieldBuffer = null;
            _forceSourceBuffer = null;
            _velocityBuffer = null;
            _densityBuffer = null;
        }

        #region Initialize

        private void InitializeBuffers()
        {
            var totalSize = (int)(_cellSize * _cellSize * _cellSize);
            _f0Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalSize * Q, sizeof(float));
            _f1Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalSize * Q, sizeof(float));
            _fieldBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalSize, sizeof(uint));
            _forceSourceBuffer =
                new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalSize, Marshal.SizeOf<float3>());
            _velocityBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalSize, Marshal.SizeOf<float3>());
            _densityBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalSize, sizeof(float));
        }

        private void InitializeShader()
        {
            ComputeShader.SetInt(_uniformMap[Uniforms.cell_size], (int)_cellSize);
            ComputeShader.SetFloat(_uniformMap[Uniforms.tau], _tau);

            ComputeShader.SetBuffer(_kernelMap[Kernels.initialize], _uniformMap[Uniforms.f0], _f0Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.initialize], _uniformMap[Uniforms.f1], _f1Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.initialize], _uniformMap[Uniforms.field], _fieldBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.initialize], _uniformMap[Uniforms.source_velocity],
                _forceSourceBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.initialize], _uniformMap[Uniforms.velocity], _velocityBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.initialize], _uniformMap[Uniforms.density], _densityBuffer);

            ComputeShader.SetBuffer(_kernelMap[Kernels.collision], _uniformMap[Uniforms.f0], _f0Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.collision], _uniformMap[Uniforms.f1], _f1Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.collision], _uniformMap[Uniforms.field], _fieldBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.collision], _uniformMap[Uniforms.source_velocity],
                _forceSourceBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.collision], _uniformMap[Uniforms.velocity], _velocityBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.collision], _uniformMap[Uniforms.density], _densityBuffer);

            ComputeShader.SetBuffer(_kernelMap[Kernels.advection], _uniformMap[Uniforms.f0], _f0Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.advection], _uniformMap[Uniforms.f1], _f1Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.advection], _uniformMap[Uniforms.field], _fieldBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.advection], _uniformMap[Uniforms.source_velocity],
                _forceSourceBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.advection], _uniformMap[Uniforms.velocity], _velocityBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.advection], _uniformMap[Uniforms.density], _densityBuffer);

            // Initialize カーネルをディスパッチ
            CalcDispatchThreadGroups(out var groupX, out var groupY, out var groupZ, _cellSize);
            ComputeShader.Dispatch(_kernelMap[Kernels.initialize], groupX, groupY, groupZ);
        }

        #endregion

        #region ComputeShader Properties

        private const int Q = 19;
        private GraphicsBuffer _f0Buffer, _f1Buffer, _fieldBuffer, _forceSourceBuffer, _velocityBuffer, _densityBuffer;

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Kernels
        {
            initialize,
            collision,
            advection
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Uniforms
        {
            cell_size,
            tau,
            f0,
            f1,
            field,
            velocity,
            density,
            source_velocity
        }

        #endregion
    }
}