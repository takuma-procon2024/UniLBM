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
            _totalParticleNum = maxPoints / threadSize * threadSize;
            _particleNum = new uint3(
                (uint)(_totalParticleNum * (dimension.x / (float)totalDimension)),
                (uint)(_totalParticleNum * (dimension.y / (float)totalDimension)),
                (uint)(_totalParticleNum * (dimension.z / (float)totalDimension))
            );
            _totalParticleNum = _particleNum.x * _particleNum.y * _particleNum.z;

            _particlesBuffer = new ComputeBuffer((int)_totalParticleNum, Marshal.SizeOf<ParticleData>());

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

            Graphics.DrawProcedural(_material, new Bounds(), MeshTopology.Points, (int)_totalParticleNum);
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
        private readonly uint _totalParticleNum;

        #endregion

        #region ComputeShader Variables

        private static readonly int ParticlesBufferPropId = Shader.PropertyToID("particles");
        private static readonly int FieldBufferPropId = Shader.PropertyToID("field");
        private static readonly int VelocityBufferPropId = Shader.PropertyToID("velocity");
        private static readonly int DimensionsPropId = Shader.PropertyToID("dimensions");
        private static readonly int DeltaTimePropId = Shader.PropertyToID("delta_time");

        private ComputeBuffer _particlesBuffer;
        private readonly ComputeBuffer _fieldBuffer;
        private readonly ComputeBuffer _velocityBuffer;
        private　readonly uint3 _gpuThreads;
        private readonly int _drawKernelId, _initKernelId;

        [StructLayout(LayoutKind.Sequential)]
        private struct ParticleData
        {
            public float3 Position;
            public float4 Color;
        }

        #endregion
    }
}