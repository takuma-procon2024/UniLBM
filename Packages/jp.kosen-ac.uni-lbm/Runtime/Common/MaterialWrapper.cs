using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Common
{
    public class MaterialWrapper<TProp> where TProp : Enum
    {
        private readonly Material _material;
        private readonly Dictionary<TProp, int> _propMap;

        public MaterialWrapper(Material material)
        {
            _material = material;
            _propMap = Enum.GetValues(typeof(TProp)).Cast<TProp>()
                .ToDictionary(t => t, t => Shader.PropertyToID(t.ToString()));
        }

        public void SetFloat(TProp prop, float value)
        {
            _material.SetFloat(_propMap[prop], value);
        }

        public void SetBool(string keyword, bool value)
        {
            if (value)
                _material.EnableKeyword(keyword);
            else
                _material.DisableKeyword(keyword);
        }

        public bool GetBool(string keyword)
        {
            return _material.IsKeywordEnabled(keyword);
        }

        public float GetFloat(TProp prop)
        {
            return _material.GetFloat(_propMap[prop]);
        }

        public void SetInt(TProp prop, int value)
        {
            _material.SetInt(_propMap[prop], value);
        }

        public void SetVector(TProp prop, in float4 value)
        {
            _material.SetVector(_propMap[prop], value);
        }

        public void SetTexture(TProp prop, Texture value)
        {
            _material.SetTexture(_propMap[prop], value);
        }

        public void SetBuffer(TProp prop, GraphicsBuffer value)
        {
            _material.SetBuffer(_propMap[prop], value);
        }
    }
}