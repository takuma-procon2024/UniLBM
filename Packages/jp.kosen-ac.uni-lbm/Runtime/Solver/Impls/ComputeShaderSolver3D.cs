using System.Collections.Generic;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Solver.Impls
{
    public class ComputeShaderSolver3D : UniLbmSolverBase
    {
        private readonly uint3 _dimensions;
        private readonly float3 _force;
        private readonly Dictionary<Kernels, int> _kernelMap;
        private readonly float _tau;

        public ComputeShaderSolver3D(ComputeShader computeShader, uint3 dimensions, float tau, float3 force)
            : base(computeShader)
        {
            _dimensions = dimensions;
            _tau = tau;
            _force = force;

            InitializeSolverBase(out _kernelMap);
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

        private void SwapBuffers()
        {
            (_f0Buffer, _f1Buffer) = (_f1Buffer, _f0Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.solve], F0PropId, _f0Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.solve], F1PropId, _f1Buffer);
        }

        public override void Step()
        {
            CalcDispatchThreadGroups(out var groupX, out var groupY, out var groupZ, _dimensions);
            ComputeShader.Dispatch(_kernelMap[Kernels.solve], groupX, groupY, groupZ);

            SwapBuffers();

            // VelocityDebugDraw();
        }

        public override ComputeBuffer GetFieldBuffer()
        {
            return _fieldBuffer;
        }

        public override ComputeBuffer GetVelocityBuffer()
        {
            return _velocityBuffer;
        }

        #region Debug

        [Conditional("UNITY_EDITOR")]
        private void VelocityDebugDraw()
        {
            var velocity = new float3[_dimensions.x * _dimensions.y * _dimensions.z];
            _velocityBuffer.GetData(velocity);

            for (var x = 0; x < _dimensions.x; x++)
            for (var y = 0; y < _dimensions.y; y++)
            for (var z = 0; z < _dimensions.z; z++)
            {
                var idx = x + y * _dimensions.x + z * _dimensions.x * _dimensions.y;
                var pos = new Vector3(x, y, z);
                var dir = new Vector3(velocity[idx].x, velocity[idx].y, velocity[idx].z);
                dir.Normalize();
                dir *= 0.9f;

                // 先端の色を変える
                Debug.DrawRay(pos, dir * 0.9f, Color.red);
                Debug.DrawRay(pos + dir * 0.9f, dir * 0.1f, Color.blue);
            }
        }

        #endregion

        #region Initialize

        private void InitializeBuffers()
        {
            var totalSize = (int)(_dimensions.x * _dimensions.y * _dimensions.z);
            _f0Buffer = new ComputeBuffer(totalSize * Q, sizeof(float));
            _f1Buffer = new ComputeBuffer(totalSize * Q, sizeof(float));
            _fieldBuffer = new ComputeBuffer(totalSize, sizeof(uint));
            _forceSourceBuffer = new ComputeBuffer(totalSize, 3 * sizeof(float));
            _velocityBuffer = new ComputeBuffer(totalSize, 3 * sizeof(float));
        }

        private void SetupShader()
        {
            ComputeShader.SetBuffer(_kernelMap[Kernels.solve], F0PropId, _f0Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.solve], F1PropId, _f1Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.solve], FieldPropId, _fieldBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.solve], ForceSourcePropId, _forceSourceBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.solve], VelocityPropId, _velocityBuffer);

            ComputeShader.SetBuffer(_kernelMap[Kernels.initialize], F0PropId, _f0Buffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.initialize], FieldPropId, _fieldBuffer);
            ComputeShader.SetBuffer(_kernelMap[Kernels.initialize], ForceSourcePropId, _forceSourceBuffer);

            ComputeShader.SetVector(DimensionsPropId, new Vector3(_dimensions.x, _dimensions.y, _dimensions.z));
            ComputeShader.SetVector(ForcePropId, new Vector4(_force.x, _force.y, _force.z));
            ComputeShader.SetFloat(TauPropId, _tau);

            // Dispatch initialization kernel
            CalcDispatchThreadGroups(out var groupX, out var groupY, out var groupZ, _dimensions);
            ComputeShader.Dispatch(_kernelMap[Kernels.initialize], groupX, groupY, groupZ);
        }

        #endregion

        #region ComputeShader properties

        private const int Q = 15;
        private static readonly int F0PropId = Shader.PropertyToID("f0");
        private static readonly int F1PropId = Shader.PropertyToID("f1");
        private static readonly int FieldPropId = Shader.PropertyToID("field");
        private static readonly int ForceSourcePropId = Shader.PropertyToID("force_source");
        private static readonly int VelocityPropId = Shader.PropertyToID("velocity");
        private static readonly int DimensionsPropId = Shader.PropertyToID("dimensions");
        private static readonly int ForcePropId = Shader.PropertyToID("force");
        private static readonly int TauPropId = Shader.PropertyToID("tau");

        private ComputeBuffer _f0Buffer, _f1Buffer, _fieldBuffer, _forceSourceBuffer, _velocityBuffer;

        private enum Kernels
        {
            // ReSharper disable once InconsistentNaming
            solve,

            // ReSharper disable once InconsistentNaming
            initialize
        }

        #endregion
    }
}