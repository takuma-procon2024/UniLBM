using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Effector.Impl
{
    public class PointEffector : IDisposable
    {
        #region Material Variables

        private static readonly int MatParticlesBufferPropId = Shader.PropertyToID("particles");

        #endregion

        public PointEffector(in uint3 dimension, uint maxPoints, ComputeShader computeShader,
            Material material, ComputeBuffer fieldBuffer, ComputeBuffer velocityBuffer)
        {
            _dimension = dimension;
            _computeShader = computeShader;
            _material = material;
            _fieldBuffer = fieldBuffer;
            _velocityBuffer = velocityBuffer;

            _initKernelId = _computeShader.FindKernel("init");
            _drawKernelId = _computeShader.FindKernel("draw");
            _computeShader.GetKernelThreadGroupSizes(_initKernelId, out var xThread, out var yThread, out var zThread);
            _gpuThreads = new uint3(xThread, yThread, zThread);

            var threadSize = _gpuThreads.x * _gpuThreads.y * _gpuThreads.z;
            var totalDimension = dimension.x * dimension.y * dimension.z;
            var totalParticleNum = maxPoints / threadSize * threadSize;
            _particleNum = new uint3(
                (uint)(totalParticleNum * (dimension.x / (float)totalDimension)),
                (uint)(totalParticleNum * (dimension.y / (float)totalDimension)),
                (uint)(totalParticleNum * (dimension.z / (float)totalDimension))
            );
            totalParticleNum = _particleNum.x * _particleNum.y * _particleNum.z;
            if (totalParticleNum <= 0)
            {
                Debug.LogError("最大パーティクル数が少なすぎます。");
                throw new ArgumentException("Invalid particle number");
            }

            _particlesBuffer = new ComputeBuffer((int)totalParticleNum, Marshal.SizeOf<ParticleData>());

            Initialize();
        }

        public void Dispose()
        {
            _particlesBuffer?.Dispose();
            _particlesBuffer = null;
        }

        private void Initialize()
        {
            _computeShader.SetVector(DimensionsPropId, new Vector4(_dimension.x, _dimension.y, _dimension.z, 0));
            _computeShader.SetInt(ParticleNumPropId, (int)_particleNum.x * (int)_particleNum.y * (int)_particleNum.z);
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
            _computeShader.SetFloat(DeltaTimePropId, Time.deltaTime);

            CalcThreadGroupSize(out var threadX, out var threadY, out var threadZ);
            _computeShader.Dispatch(_drawKernelId, threadX, threadY, threadZ);

            var bound = new Bounds(Vector3.zero, new Vector3(_dimension.x, _dimension.y, _dimension.z));
            Graphics.DrawProcedural(_material, bound, MeshTopology.Points, _particlesBuffer.count);
        }

        private void CalcThreadGroupSize(out int x, out int y, out int z)
        {
            x = (int)math.ceil((float)_particleNum.x / _gpuThreads.x);
            y = (int)math.ceil((float)_particleNum.y / _gpuThreads.y);
            z = (int)math.ceil((float)_particleNum.z / _gpuThreads.z);
        }


        #region Variables

        private readonly ComputeShader _computeShader;
        private readonly Material _material;
        private readonly uint3 _dimension;
        private readonly uint3 _particleNum;

        #endregion

        #region ComputeShader Variables

        private static readonly int ParticlesBufferPropId = Shader.PropertyToID("particles");
        private static readonly int FieldBufferPropId = Shader.PropertyToID("field");
        private static readonly int VelocityBufferPropId = Shader.PropertyToID("velocity");
        private static readonly int DimensionsPropId = Shader.PropertyToID("dimensions");
        private static readonly int DeltaTimePropId = Shader.PropertyToID("delta_time");
        private static readonly int ParticleNumPropId = Shader.PropertyToID("particle_num");

        private ComputeBuffer _particlesBuffer;
        private readonly ComputeBuffer _fieldBuffer;
        private readonly ComputeBuffer _velocityBuffer;
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