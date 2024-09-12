using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Mathematics;
using UnityEngine;

namespace Solver.Impls
{
    public class ComputeD3Q19Solver : UniLbmSolverBase
    {
        public ComputeD3Q19Solver(ComputeShader computeShader, float tau, uint3 cellSize) : base(computeShader)
        {
            _tau = tau;
            _cellSize = cellSize;

            InitializeSolverBase(out _kernelMap, out _uniformMap);
            InitializeBuffers();
            InitializeShader();
        }

        public override void Step()
        {
            CalcDispatchThreadGroups(out var groupX, out var groupY, out var groupZ, _cellSize);
            ComputeShader.Dispatch(_kernelMap[Kernels.collision], groupX, groupY, groupZ);
            ComputeShader.Dispatch(_kernelMap[Kernels.advection], groupX, groupY, groupZ);
            
            SwapBuffers();
        }

        public override ComputeBuffer GetFieldBuffer()
        {
            return _currentBuffer;
        }

        public override ComputeBuffer GetVelocityBuffer()
        {
            return _particleBuffer;
        }

        private void SwapBuffers()
        {
            (_currentBuffer, _nextBuffer) = (_nextBuffer, _currentBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.collision], _uniformMap[Uniforms.current], _currentBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.collision], _uniformMap[Uniforms.next], _nextBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.advection], _uniformMap[Uniforms.current], _currentBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.advection], _uniformMap[Uniforms.next], _nextBuffer);
        }

        public override void Dispose()
        {
            base.Dispose();

            _currentBuffer?.Dispose();
            _nextBuffer?.Dispose();
            _particleBuffer?.Dispose();
            _inputVelocityBuffer?.Dispose();

            _currentBuffer = null;
            _nextBuffer = null;
            _particleBuffer = null;
            _inputVelocityBuffer = null;
        }

        #region Initialize

        private void InitializeBuffers()
        {
            var size = (int)(_cellSize.x * _cellSize.y * _cellSize.z);
            _currentBuffer = new ComputeBuffer(size, sizeof(float) * 19);
            _nextBuffer = new ComputeBuffer(size, sizeof(float) * 19);
            _particleBuffer = new ComputeBuffer(size, sizeof(float) * 4);
            _inputVelocityBuffer = new ComputeBuffer(size, sizeof(float) * 3);
        }

        private void InitializeShader()
        {
            ComputeShader.SetVector(_uniformMap[Uniforms.cell_size],
                new Vector3(_cellSize.x, _cellSize.y, _cellSize.z));
            ComputeShader.SetFloat(_uniformMap[Uniforms.tau], _tau);

            SetBuffers(Kernels.initialize);
            SetBuffers(Kernels.collision);
            SetBuffers(Kernels.advection);

            CalcDispatchThreadGroups(out var groupX, out var groupY, out var groupZ, _cellSize);
            ComputeShader.Dispatch(_kernelMap[Kernels.initialize], groupX, groupY, groupZ);

            return;

            void SetBuffers(in Kernels kernel)
            {
                ComputeShader.SetBuffer(_kernelMap[kernel], _uniformMap[Uniforms.current], _currentBuffer);
                ComputeShader.SetBuffer(_kernelMap[kernel], _uniformMap[Uniforms.next], _nextBuffer);
                ComputeShader.SetBuffer(_kernelMap[kernel], _uniformMap[Uniforms.particle], _particleBuffer);
                ComputeShader.SetBuffer(_kernelMap[kernel], _uniformMap[Uniforms.input_velocity], _inputVelocityBuffer);
            }
        }

        #endregion

        #region Variables

        private ComputeBuffer _currentBuffer,
            _nextBuffer,
            _particleBuffer,
            _inputVelocityBuffer;

        private readonly float _tau;
        private readonly uint3 _cellSize;

        #endregion

        #region ComputeShader Variables

        private readonly Dictionary<Uniforms, int> _uniformMap;
        private readonly Dictionary<Kernels, int> _kernelMap;

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Uniforms
        {
            cell_size,
            tau,
            current,
            next,
            particle,
            input_velocity
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Kernels
        {
            initialize,
            collision,
            advection
        }

        #endregion
    }
}