using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UniLbm.Common;
using UniLbm.Lbm.Extension;
using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Lbm
{
    /// <summary>
    ///     ToFセンサからのデータを管理して反映するクラス
    /// </summary>
    public class TofSensorManager
    {
        private readonly ComputeShaderWrapper<Kernels, Uniforms> _shader;
        private readonly ILbmToFSensor[] _tofSensors;

        public TofSensorManager(ComputeShader shader, GameObject tofSensorRoot)
        {
            _shader = new ComputeShaderWrapper<Kernels, Uniforms>(shader);
            _tofSensors = tofSensorRoot.GetComponentsInChildren<ILbmToFSensor>();
            if (_tofSensors.Length == 0)
            {
                Debug.LogWarning("LbmToFSensorが見つかりませんでした");
                return;
            }

            SetBuffers();
        }

        #region ComputeShader

        private GraphicsBuffer _tofDataBuffer;

        private void InitBuffers()
        {
            _tofDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _tofSensors.Length,
                Marshal.SizeOf<ToFData>());
        }

        private void SetBuffers()
        {
            _shader.SetBuffer(Kernels.set_velocity, Uniforms.tof_data_buffer, _tofDataBuffer);
        }

        private void SetData()
        {
            _shader.SetInt(Uniforms.tof_data_count, _tofSensors.Length);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Kernels
        {
            set_velocity
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Uniforms
        {
            tof_data_buffer,
            tof_data_count
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ToFData
        {
            public float distance;
            public float3 position;
            public float3 direction;
        }

        #endregion
    }
}