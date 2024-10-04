using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Common
{
    public static class RenderTexUtil
    {
        public static RenderTexture Create2D(in int2 res, in RenderTextureFormat format = RenderTextureFormat.ARGBFloat,
            in FilterMode filter = FilterMode.Point)
        {
            var buffer = new RenderTexture(res.x, res.y, 0, format)
            {
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                enableRandomWrite = true
            };
            buffer.Create();
            return buffer;
        }
    }
}