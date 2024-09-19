using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Common
{
    public class ComputeShaderWrapper<TKernel, TUniform> where TKernel : Enum where TUniform : Enum
    {
        public readonly Dictionary<TKernel, int> KernelMap;
        public readonly Dictionary<TUniform, int> UniformMap;

        public ComputeShaderWrapper(ComputeShader shader)
        {
            KernelMap = Enum.GetValues(typeof(TKernel))
                .Cast<TKernel>()
                .ToDictionary(t => t, t => shader.FindKernel(t.ToString()));
            UniformMap = Enum.GetValues(typeof(TUniform))
                .Cast<TUniform>()
                .ToDictionary(t => t, t => Shader.PropertyToID(t.ToString()));
        }
    }
}