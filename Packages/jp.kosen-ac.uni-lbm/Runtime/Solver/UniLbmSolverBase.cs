using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Solver
{
    /// <summary>
    ///     ソルバーの基底クラス
    /// </summary>
    public abstract class UniLbmSolverBase : IDisposable
    {
        protected UniLbmSolverBase(ComputeShader computeShader)
        {
            ComputeShader = computeShader;
        }

        public virtual void Dispose()
        {
        }

        public virtual void Release()
        {
        }

        protected void InitializeSolverBase<TKernels>(out Dictionary<TKernels, int> kernelMap) where TKernels : Enum
        {
            InitialCheck();

            kernelMap = Enum.GetValues(typeof(TKernels))
                .Cast<TKernels>()
                .ToDictionary(
                    kernel => kernel,
                    kernel => ComputeShader.FindKernel(kernel.ToString())
                );

            ComputeShader.GetKernelThreadGroupSizes(
                kernelMap.First().Value,
                out var threadX, out var threadY, out var threadZ);
            _gpuThreads = new uint3(threadX, threadY, threadZ);
        }

        private void InitialCheck()
        {
            Assert.IsTrue(SystemInfo.supportsComputeShaders, "ComputeShader is not supported : UniLBM");
            Assert.IsNotNull(ComputeShader, "ComputeShader is not set : UniLBM");
            Assert.IsTrue(SystemInfo.graphicsShaderLevel >= 50,
                "Under the DirectCompute5.0 (DX11 GPU) doesn't work : UniLBM");
            Assert.IsTrue(_gpuThreads.x * _gpuThreads.y * _gpuThreads.z <= DirectCompute5_0.MaxProcess,
                "Resolution is too height : UniLBM");
            Assert.IsTrue(_gpuThreads.x <= DirectCompute5_0.MaxX, "THREAD_X is too large : UniLBM");
            Assert.IsTrue(_gpuThreads.y <= DirectCompute5_0.MaxY, "THREAD_Y is too large : UniLBM");
            Assert.IsTrue(_gpuThreads.z <= DirectCompute5_0.MaxZ, "THREAD_Z is too large : UniLBM");
        }

        protected void CalcDispatchThreadGroups(out int x, out int y, out int z, in uint3 dimensions)
        {
            x = (int)math.ceil((float)dimensions.x / _gpuThreads.x);
            y = (int)math.ceil((float)dimensions.y / _gpuThreads.y);
            z = (int)math.ceil((float)dimensions.z / _gpuThreads.z);
        }

        public abstract void Step();
        
        public abstract ComputeBuffer GetFieldBuffer();
        public abstract ComputeBuffer GetVelocityBuffer();

        #region Variables

        protected readonly ComputeShader ComputeShader;
        private uint3 _gpuThreads;

        #endregion
    }

    // ReSharper disable once InconsistentNaming
    internal static class DirectCompute5_0
    {
        private const int MaxThread = 1024;
        public const int MaxX = 1024;
        public const int MaxY = 1024;
        public const int MaxZ = 64;
        private const int MaxDispatch = 65535;
        public const int MaxProcess = MaxDispatch * MaxThread;
    }
}