using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Effector.Impl
{
    public class PointEffector : IDisposable
    {
        public PointEffector(in uint cellSize, uint maxPoints, ComputeShader computeShader,
            Material material, GraphicsBuffer fieldBuffer, GraphicsBuffer velocityBuffer)
        {
            _cellSize = cellSize;
            _computeShader = computeShader;
            _material = material;
            _fieldBuffer = fieldBuffer;
            _velocityBuffer = velocityBuffer;

            _initKernelId = _computeShader.FindKernel("init");
            _drawKernelId = _computeShader.FindKernel("draw");
            _computeShader.GetKernelThreadGroupSizes(_initKernelId, out var xThread, out var yThread, out var zThread);
            Assert.IsTrue(xThread == yThread && yThread == zThread, "スレッド数が一致しません。");
            _gpuThreads = new uint3(xThread, yThread, zThread);

            var cbrtParticleNum = (uint)math.floor(math.pow(maxPoints, 1f / 3f));
            _particleNum = cbrtParticleNum / xThread * xThread;
            Assert.IsTrue(_particleNum.x > 0, "パーティクル数が少なすぎます。");

            _particlesBuffer = new ComputeBuffer((int)math.pow(_particleNum.x, 3), Marshal.SizeOf<ParticleData>());

            var materialSize = material.GetFloat(MatSizePropId);
            var bound = new Bounds(Vector3.zero, new Vector3(materialSize, materialSize, materialSize));
            _renderParams = new RenderParams(material)
            {
                worldBounds = bound
            };

            Initialize();
        }

        public float MoveSpeed { get; set; } = 500f;

        public void Dispose()
        {
            _particlesBuffer?.Dispose();
            _particlesBuffer = null;
        }

        /// <summary>
        ///     パーティクルの色を制御するパラメーターを設定する
        /// </summary>
        /// <param name="hueSpeed">色相の変化倍率</param>
        /// <param name="s">彩度</param>
        /// <param name="v">明度</param>
        /// <param name="alpha">出力色のAlpha値</param>
        public void SetHsvParam(float hueSpeed = 100f, float s = 1f, float v = 1f, float alpha = 1f)
        {
            _computeShader.SetVector(HsvParamPropId, new Vector4(hueSpeed, s, v, alpha));
        }

        private void Initialize()
        {
            SetHsvParam();
            _computeShader.SetInt(CellSizePropId, (int)_cellSize);
            _computeShader.SetInt(ParticleNumPropId, (int)_particleNum.x);
            _computeShader.SetBuffer(_initKernelId, ParticlesBufferPropId, _particlesBuffer);
            _computeShader.SetBuffer(_drawKernelId, ParticlesBufferPropId, _particlesBuffer);
            _computeShader.SetBuffer(_drawKernelId, FieldBufferPropId, _fieldBuffer);
            _computeShader.SetBuffer(_drawKernelId, VelocityBufferPropId, _velocityBuffer);

            _material.SetBuffer(MatParticlesBufferPropId, _particlesBuffer);

            CalcThreadGroupSize(out var threadX, out var threadY, out var threadZ);
            _computeShader.Dispatch(_initKernelId, threadX, threadY, threadZ);
        }

        public void Update()
        {
            _computeShader.SetFloat(DeltaTimePropId, 0.02f * MoveSpeed);

            CalcThreadGroupSize(out var threadX, out var threadY, out var threadZ);
            _computeShader.Dispatch(_drawKernelId, threadX, threadY, threadZ);
            
            Graphics.RenderPrimitives(_renderParams, MeshTopology.Points, _particlesBuffer.count);
        }

        private void CalcThreadGroupSize(out int x, out int y, out int z)
        {
            x = (int)math.ceil((float)_particleNum.x / _gpuThreads.x);
            y = (int)math.ceil((float)_particleNum.y / _gpuThreads.y);
            z = (int)math.ceil((float)_particleNum.z / _gpuThreads.z);
        }

        #region Material Variables

        private static readonly int MatParticlesBufferPropId = Shader.PropertyToID("particles");
        private static readonly int MatSizePropId = Shader.PropertyToID("size");

        #endregion


        #region Variables

        private readonly ComputeShader _computeShader;
        private readonly Material _material;
        private readonly uint _cellSize;
        private readonly uint3 _particleNum;
        private readonly RenderParams _renderParams;

        #endregion

        #region ComputeShader Variables

        private static readonly int ParticlesBufferPropId = Shader.PropertyToID("particles");
        private static readonly int FieldBufferPropId = Shader.PropertyToID("field");
        private static readonly int VelocityBufferPropId = Shader.PropertyToID("velocity");
        private static readonly int CellSizePropId = Shader.PropertyToID("cell_size");
        private static readonly int DeltaTimePropId = Shader.PropertyToID("delta_time");
        private static readonly int ParticleNumPropId = Shader.PropertyToID("num_particles");
        private static readonly int HsvParamPropId = Shader.PropertyToID("hsv_param");

        private ComputeBuffer _particlesBuffer;
        private readonly GraphicsBuffer _fieldBuffer;
        private readonly GraphicsBuffer _velocityBuffer;
        private　readonly uint3 _gpuThreads;
        private readonly int _drawKernelId, _initKernelId;

        [StructLayout(LayoutKind.Sequential)]
        private struct ParticleData
        {
            public float3 Position;
            public float3 PrevPos;
            public float4 Color;
        }

        #endregion
    }
}