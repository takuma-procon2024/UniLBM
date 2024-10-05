using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UniLbm.Cloth;
using UniLbm.Common;
using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Lbm
{
    /// <summary>
    ///     D3Q19 LBM ソルバー
    /// </summary>
    public class D3Q19LbmSolver : ILbmSolver
    {
        private readonly ClothSolver _clothSolver;
        private readonly ComputeShaderWrapper<Kernels, Uniforms> _shader;

        public D3Q19LbmSolver(ComputeShader shader, ClothSolver clothSolver, uint cellRes, in Data data)
        {
            _shader = new ComputeShaderWrapper<Kernels, Uniforms>(shader);
            CellRes = (int)cellRes;
            _clothSolver = clothSolver;

            InitBuffers();
            SetBuffers();
            SetData(in data);
            DispatchInit();
        }

        public GraphicsBuffer FieldVelocityBuffer { get; private set; }

        public void Dispose()
        {
            _f0Buffer?.Dispose();
            _f1Buffer?.Dispose();
            FieldBuffer?.Dispose();
            ExternalForceBuffer?.Dispose();
            VelDensBuffer?.Dispose();
            FieldVelocityBuffer?.Dispose();
        }

        public GraphicsBuffer VelDensBuffer { get; private set; }

        public GraphicsBuffer FieldBuffer { get; private set; }
        public GraphicsBuffer ExternalForceBuffer { get; private set; }

        public int CellRes { get; }

        public void Update()
        {
            DispatchSimulate();

            // f0とf1をスワップ
            (_f0Buffer, _f1Buffer) = (_f1Buffer, _f0Buffer);
            _shader.SetBuffer(new[] { Kernels.collision, Kernels.advection }, Uniforms.f0, _f0Buffer);
            _shader.SetBuffer(new[] { Kernels.collision, Kernels.advection }, Uniforms.f1, _f1Buffer);
        }

        public void ResetField()
        {
            DispatchResetVelocity();
        }

        #region ComputeShader

        private const int Q = 19;
        private GraphicsBuffer _f0Buffer, _f1Buffer;

        private void InitBuffers()
        {
            var cellCnt = CellRes * CellRes * CellRes;
            _f0Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, cellCnt * Q, sizeof(float));
            _f1Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, cellCnt * Q, sizeof(float));
            FieldBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, cellCnt, sizeof(uint));
            FieldVelocityBuffer =
                new GraphicsBuffer(GraphicsBuffer.Target.Structured, cellCnt, Marshal.SizeOf<float3>());
            ExternalForceBuffer =
                new GraphicsBuffer(GraphicsBuffer.Target.Structured, cellCnt, Marshal.SizeOf<float3>());
            VelDensBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, cellCnt, Marshal.SizeOf<float4>());
        }

        private void SetBuffers()
        {
            var lbmKernels = new[] { Kernels.initialize, Kernels.collision, Kernels.advection };
            var allKernels = new[] { Kernels.initialize, Kernels.collision, Kernels.advection, Kernels.reset_field };
            var advectionFieldKernels = new[] { Kernels.advection, Kernels.reset_field };

            _shader.SetBuffer(allKernels, Uniforms.field, FieldBuffer);

            _shader.SetBuffer(lbmKernels, Uniforms.f0, _f0Buffer);
            _shader.SetBuffer(lbmKernels, Uniforms.f1, _f1Buffer);
            _shader.SetBuffer(lbmKernels, Uniforms.external_force, ExternalForceBuffer);
            _shader.SetBuffer(lbmKernels, Uniforms.vel_dens, VelDensBuffer);

            _shader.SetBuffer(advectionFieldKernels, Uniforms.field_velocity, FieldVelocityBuffer);

            _shader.SetTexture(Kernels.advection, Uniforms.cloth_force_tex, _clothSolver.ExternalForceBuffer);
        }

        public void SetData(in Data data)
        {
            _shader.SetInt(Uniforms.cell_res_int, CellRes);
            _shader.SetFloat(Uniforms.tau, data.Tau);
        }

        private void DispatchInit()
        {
            _shader.Dispatch(Kernels.initialize, (uint)CellRes);
        }

        private void DispatchSimulate()
        {
            _shader.Dispatch(Kernels.collision, (uint)CellRes);
            _shader.Dispatch(Kernels.advection, (uint)CellRes);
        }

        private void DispatchResetVelocity()
        {
            _shader.Dispatch(Kernels.reset_field, (uint)CellRes);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Kernels
        {
            initialize,
            collision,
            advection,
            reset_field
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Uniforms
        {
            f0,
            f1,
            field,
            field_velocity,
            cloth_force_tex,
            external_force,
            vel_dens,
            tau,
            cell_res_int
        }

        public readonly struct Data
        {
            public float Tau { get; init; }
        }

        #endregion
    }
}