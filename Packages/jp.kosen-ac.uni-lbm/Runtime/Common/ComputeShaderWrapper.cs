using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UniLbm.Common
{
    /// <summary>
    ///     コンピュートシェーダーを便利に使えるようにするクラス
    /// </summary>
    public class ComputeShaderWrapper<TKernel, TUniform> where TKernel : Enum where TUniform : Enum
    {
        private readonly Dictionary<TKernel, int> _kernelMap;
        private readonly ComputeShader _shader;
        private readonly uint3[] _threadGroupSizeMap;
        private readonly Dictionary<TUniform, int> _uniformMap;

        public ComputeShaderWrapper(ComputeShader shader)
        {
            _shader = shader;

            _kernelMap = Enum.GetValues(typeof(TKernel)).Cast<TKernel>()
                .ToDictionary(t => t, t => _shader.FindKernel(t.ToString()));
            _threadGroupSizeMap = _kernelMap.Values.Select(id =>
            {
                _shader.GetKernelThreadGroupSizes(id, out var x, out var y, out var z);
                return new uint3(x, y, z);
            }).ToArray();
            _uniformMap = Enum.GetValues(typeof(TUniform)).Cast<TUniform>()
                .ToDictionary(t => t, t => Shader.PropertyToID(t.ToString()));
        }

        public void Dispatch(TKernel kernel, in uint3 threadSize)
        {
            var threadGroupSize = _threadGroupSizeMap[_kernelMap[kernel]];

#if UNITY_EDITOR
            Assert.IsTrue(
                threadSize.x % threadGroupSize.x == 0
                && threadSize.y % threadGroupSize.y == 0
                && threadSize.z % threadGroupSize.z == 0,
                "パフォーマンスの観点から、スレッドサイズはスレッドグループの倍数にしてください");
#endif

            var threadGroups = new int3(math.ceil(new float3(threadSize) / threadGroupSize));

            _shader.Dispatch(_kernelMap[kernel], threadGroups.x, threadGroups.y, threadGroups.z);
        }

        public void SetBuffer(TKernel kernel, TUniform uniform, GraphicsBuffer buffer)
        {
            _shader.SetBuffer(_kernelMap[kernel], _uniformMap[uniform], buffer);
        }

        public void SetBuffer(TKernel[] kernels, TUniform uniform, GraphicsBuffer buffer)
        {
            foreach (var kernel in kernels) SetBuffer(kernel, uniform, buffer);
        }

        public void SetTexture(TKernel kernel, TUniform uniform, RenderTexture texture)
        {
            _shader.SetTexture(_kernelMap[kernel], _uniformMap[uniform], texture);
        }

        public void SetTexture(TKernel[] kernels, TUniform uniform, RenderTexture texture)
        {
            foreach (var kernel in kernels) SetTexture(kernel, uniform, texture);
        }

        public void SetVector(TUniform uniform, in float4 vector)
        {
            _shader.SetVector(_uniformMap[uniform], vector);
        }

        public void SetInt(TUniform uniform, int value)
        {
            _shader.SetInt(_uniformMap[uniform], value);
        }

        public void SetFloat(TUniform uniform, float value)
        {
            _shader.SetFloat(_uniformMap[uniform], value);
        }
        
        public static RenderTexture CreateRT3D(in int3 res, RenderTextureFormat format, FilterMode filterMode)
        {
            var rt = new RenderTexture(res.x, res.y, 0, format)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = res.z,
                enableRandomWrite = true,
                filterMode = filterMode,
                wrapMode = TextureWrapMode.Clamp,
            };
            rt.Create();
            return rt;
        }
    }
}