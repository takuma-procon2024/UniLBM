using System.Diagnostics.CodeAnalysis;
using UniLbm.Common;
using UnityEngine;

namespace UniLbm.Cloth
{
    public static class UnlitClothRenderer
    {
        public static void Initialize(Material material, GameObject go, GameObject clothRendererGo, ClothSolver solver)
        {
            var mat = new MaterialWrapper<Props>(material);

            var meshRenderer = go.AddComponent<MeshRenderer>();
            var meshFilter = go.AddComponent<MeshFilter>();

            meshRenderer.material = material;
            mat.SetTexture(Props._position_tex, solver.PositionBuffer);

            clothRendererGo.TryGetComponent(out MeshFilter clothMeshFilter);
            // 布のレンダラーから同一のメッシュを共有する
            meshFilter.sharedMesh = clothMeshFilter.sharedMesh;
        }

        #region Material

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Props
        {
            _position_tex
        }

        #endregion
    }
}