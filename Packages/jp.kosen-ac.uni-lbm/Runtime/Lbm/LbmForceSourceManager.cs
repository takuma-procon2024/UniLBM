using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UniLbm.Common;
using UniLbm.Lbm.Behaviours;
using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Lbm
{
    /// <summary>
    ///     力源の管理を行うクラス
    /// </summary>
    /// <remarks>
    ///     コンストラクタを呼んだ時点で、シーン上に存在している`LbmForceSource`
    ///     を収集してくるためコンストラクタを呼ぶタイミングに気を付ける
    /// </remarks>
    public class LbmForceSourceManager
    {
        private readonly LbmForceSource[] _sources = Object.FindObjectsByType<LbmForceSource>(FindObjectsSortMode.None);
        private readonly ComputeShaderWrapper<Kernels, Uniforms> _shader;

        public LbmForceSourceManager(ComputeShader shader, ILbmSolver lbmSolver)
        {
            _shader = new ComputeShaderWrapper<Kernels, Uniforms>(shader);
            
            InitBuffers();
            SetBuffers(lbmSolver);
            SetData(lbmSolver);
        }

        #region ComputeShader

        private GraphicsBuffer _forcesBuffer;

        private void InitBuffers()
        {
            _forcesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _sources.Length,
                Marshal.SizeOf<SourceData>());
        }

        private void SetBuffers(ILbmSolver lbmSolver)
        {
            _shader.SetBuffer(Kernels.set_powers, Uniforms.lbm_external_force, lbmSolver.ExternalForceBuffer);
            _shader.SetBuffer(Kernels.set_powers, Uniforms.power_source, _forcesBuffer);
        }

        private void SetData(ILbmSolver lbmSolver)
        {
            _shader.SetInt(Uniforms.lbm_res, lbmSolver.CellRes);
            _shader.SetInt(Uniforms.power_source_count, _sources.Length);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Kernels
        {
            set_powers
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Uniforms
        {
            power_source,
            power_source_count,
            lbm_external_force,
            lbm_res
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct SourceData
        {
            public readonly float3 power;
            public readonly float4x4 transform;
        }

        #endregion
    }
}