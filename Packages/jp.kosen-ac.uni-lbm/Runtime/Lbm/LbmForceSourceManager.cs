using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UniLbm.Common;
using UniLbm.Lbm.Behaviours;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UniLbm.Lbm
{
    /// <summary>
    ///     力源の管理を行うクラス
    /// </summary>
    /// <remarks>
    ///     コンストラクタを呼んだ時点で、シーン上に存在している`LbmForceSource`
    ///     を収集してくるためコンストラクタを呼ぶタイミングに気を付ける
    /// </remarks>
    public class LbmForceSourceManager : IDisposable
    {
        private readonly ComputeShaderWrapper<Kernels, Uniforms> _shader;
        private readonly LbmForceSource[] _sources = Object.FindObjectsByType<LbmForceSource>(FindObjectsSortMode.None);

        public LbmForceSourceManager(ComputeShader shader, ILbmSolver lbmSolver, LbmParticle particle)
        {
            if (_sources.Length == 0)
            {
                Debug.LogWarning("LbmForceSourceが見つかりませんでした");
                return;
            }
            
            _shader = new ComputeShaderWrapper<Kernels, Uniforms>(shader);

            InitBuffers();
            SetBuffers(lbmSolver);
            SetData(lbmSolver, particle);
        }

        public void Dispose()
        {
            _sourcesBuffer?.Dispose();
        }

        public void Update()
        {
            if (_sources.Length == 0) return;

            // データを設定
            var data = new NativeArray<SourceData>(_sources.Length, Allocator.Temp);
            for (var i = 0; i < _sources.Length; i++)
            {
                var source = _sources[i];
                var srcTrs = source.transform;
                data[i] = new SourceData(source.Force, srcTrs.position, source.CellSize);
            }

            _sourcesBuffer.SetData(data);
            data.Dispose();

            // ComputeShaderを実行
            var dispatchCount = (uint)(math.ceil(_sources.Length / 4f) * 4);
            _shader.Dispatch(Kernels.set_powers, new uint3(dispatchCount, 1, 1));
        }

        #region ComputeShader

        private GraphicsBuffer _sourcesBuffer;

        private void InitBuffers()
        {
            _sourcesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _sources.Length,
                Marshal.SizeOf<SourceData>());
        }

        private void SetBuffers(ILbmSolver lbmSolver)
        {
            _shader.SetBuffer(Kernels.set_powers, Uniforms.lbm_external_force, lbmSolver.ExternalForceBuffer);
            _shader.SetBuffer(Kernels.set_powers, Uniforms.sources_buffer, _sourcesBuffer);
        }

        private void SetData(ILbmSolver lbmSolver, LbmParticle particle)
        {
            _shader.SetInt(Uniforms.lbm_res, lbmSolver.CellRes);
            _shader.SetInt(Uniforms.source_count, _sources.Length);
            _shader.SetInt(Uniforms.lbm_boundary_size, particle.Bounds);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Kernels
        {
            set_powers
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Uniforms
        {
            sources_buffer,
            source_count,
            lbm_external_force,
            lbm_res,
            lbm_boundary_size
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct SourceData
        {
            public readonly float3 power;
            public readonly float3 position;
            public readonly uint3 cellSize;

            public SourceData(in float3 power, in float3 position, in uint3 cellSize)
            {
                this.power = power;
                this.position = position;
                this.cellSize = cellSize;
            }
        }

        #endregion
    }
}