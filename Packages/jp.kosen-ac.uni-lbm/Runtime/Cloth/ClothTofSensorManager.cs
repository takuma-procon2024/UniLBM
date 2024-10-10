using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UniLbm.Cloth.Extension;
using UniLbm.Common;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Cloth
{
    /// <summary>
    ///     ToFセンサからのデータを管理して反映するクラス
    /// </summary>
    public class ClothTofSensorManager : IDisposable
    {
        private readonly ClothSolver _clothSolver;
        private readonly ComputeShaderWrapper<Kernels, Uniforms> _shader;
        private readonly ILbmToFSensor[] _tofSensors;

        public ClothTofSensorManager(ComputeShader shader, GameObject tofSensorRoot, ClothSolver clothSolver,
            in Data data)
        {
            _shader = new ComputeShaderWrapper<Kernels, Uniforms>(shader);
            _clothSolver = clothSolver;
            _tofSensors = tofSensorRoot.GetComponentsInChildren<ILbmToFSensor>();
            if (_tofSensors.Length == 0)
            {
                Debug.LogWarning("ToFSensorオブジェクトが見つかりませんでした");
                return;
            }

            InitBuffers();
            SetBuffers(_clothSolver);
            SetData(in data);
        }

        public void Dispose()
        {
            _tofDataBuffer?.Dispose();
        }

        public void Update()
        {
            if (_tofSensors.Length == 0) return;

            SetSensorDataToBuffer();

            var clothRes = new uint2(_clothSolver.ClothResolution);
            var tofLength = (uint)math.ceil(_tofSensors.Length / 8f) * 8;
            _shader.Dispatch(Kernels.write_tof_data, new uint3(clothRes, tofLength));
        }

        #region ComputeShader

        private GraphicsBuffer _tofDataBuffer;

        private void InitBuffers()
        {
            _tofDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _tofSensors.Length,
                Marshal.SizeOf<ToFData>());
        }

        private void SetSensorDataToBuffer()
        {
            var tofData = new NativeArray<ToFData>(_tofSensors.Length, Allocator.Temp);
            for (var i = 0; i < _tofSensors.Length; i++)
            {
                var sensor = _tofSensors[i];
                tofData[i] = new ToFData
                {
                    distance = sensor.Distance,
                    position = new float2(sensor.Position.x, sensor.Position.y)
                };
            }

            _tofDataBuffer.SetData(tofData);
        }

        private void SetBuffers(ClothSolver clothSolver)
        {
            _shader.SetBuffer(Kernels.write_tof_data, Uniforms.tof_buffer, _tofDataBuffer);
            _shader.SetTexture(Kernels.write_tof_data, Uniforms.cloth_pos_buffer, clothSolver.PositionBuffer);
            _shader.SetTexture(Kernels.write_tof_data, Uniforms.cloth_external_buffer, clothSolver.ExternalForceBuffer);
        }

        public void SetData(in Data data)
        {
            _shader.SetFloat(Uniforms.tof_radius, data.TofRadius);
            _shader.SetMatrix(Uniforms.cloth_transform, data.ClothTransform);
            _shader.SetFloat(Uniforms.tof_default_distance, data.TofDefaultDistance);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Kernels
        {
            write_tof_data
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Uniforms
        {
            tof_buffer,
            cloth_pos_buffer,
            cloth_external_buffer,
            tof_radius,
            cloth_transform,
            tof_default_distance
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ToFData
        {
            public float distance;
            public float2 position;
        }

        public readonly struct Data
        {
            public float TofRadius { get; init; }
            public float4x4 ClothTransform { get; init; }
            public float TofDefaultDistance { get; init; }
        }

        #endregion
    }
}