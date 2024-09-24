using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Lbm
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

        protected void InitializeSolverBase<TKernels, TUniform>(out Dictionary<TKernels, int> kernelMap,
            out Dictionary<TUniform, int> uniformMap) where TKernels : Enum where TUniform : Enum
        {
            InitialCheck();

            kernelMap = CreateKernelMap<TKernels>(ComputeShader);
            uniformMap = CreateUniformMap<TUniform>();

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

        protected void CalcDispatchThreadGroups(out int x, out int y, out int z, in uint cellSize)
        {
            x = (int)math.ceil((float)cellSize / _gpuThreads.x);
            y = (int)math.ceil((float)cellSize / _gpuThreads.y);
            z = (int)math.ceil((float)cellSize / _gpuThreads.z);
        }

        private static Dictionary<T, int> CreateUniformMap<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T))
                .Cast<T>()
                .ToDictionary(u => u, u => Shader.PropertyToID(u.ToString()));
        }

        private static Dictionary<T, int> CreateKernelMap<T>(ComputeShader shader) where T : Enum
        {
            return Enum.GetValues(typeof(T))
                .Cast<T>()
                .ToDictionary(k => k, k => shader.FindKernel(k.ToString()));
        }

        public abstract void Step();

        public abstract GraphicsBuffer GetFieldBuffer();
        public abstract GraphicsBuffer GetVelocityBuffer();
        public abstract GraphicsBuffer GetExternalForceBuffer();
        public abstract uint GetCellSize();

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