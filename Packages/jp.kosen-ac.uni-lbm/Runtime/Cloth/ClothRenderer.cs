using System.Diagnostics.CodeAnalysis;
using UniLbm.Common;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace UniLbm.Cloth
{
    public static class ClothRenderer
    {
        public static void Initialize(Material material, GameObject go, ClothSolver solver)
        {
            var mat = new MaterialWrapper<Props>(material);

            var meshRenderer = go.AddComponent<MeshRenderer>();
            var meshFilter = go.AddComponent<MeshFilter>();

            meshRenderer.material = material;
            mat.SetTexture(Props._position_tex, solver.PositionBuffer);
            mat.SetTexture(Props._normal_tex, solver.NormalBuffer);
            mat.SetTexture(Props._external_force_tex, solver.ExternalForceBuffer);

            meshFilter.mesh = new Mesh();
            GenerateMesh(meshFilter.mesh, meshRenderer, solver);
        }

        private static void GenerateMesh(Mesh mesh, MeshRenderer renderer, ClothSolver simulation)
        {
            var clothRes = simulation.ClothResolution;
            var gridSize = clothRes - 1;

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var tileSize = 1f / new float2(gridSize);

            var vertCnt = clothRes.x * clothRes.y * 4;
            var trianglesCnt = clothRes.x * clothRes.y * 6;

            var vertices = new NativeArray<float3>(vertCnt, Allocator.Temp);
            var triangles = new NativeArray<uint>(trianglesCnt, Allocator.Temp);
            var normals = new NativeArray<float3>(vertCnt, Allocator.Temp);
            var uvs = new NativeArray<float2>(vertCnt, Allocator.Temp);

            var triangleIndex = 0;
            var index = 0;
            for (var y = 0; y < clothRes.y; y++)
            for (var x = 0; x < clothRes.x; x++)
            {
                vertices[index + 0] = new float3((x + 0) * tileSize.x, (y + 0) * tileSize.y, 0);
                vertices[index + 1] = new float3((x + 1) * tileSize.x, (y + 0) * tileSize.y, 0);
                vertices[index + 2] = new float3((x + 1) * tileSize.x, (y + 1) * tileSize.y, 0);
                vertices[index + 3] = new float3((x + 0) * tileSize.x, (y + 1) * tileSize.y, 0);

                triangles[triangleIndex + 0] = (uint)(index + 2);
                triangles[triangleIndex + 1] = (uint)(index + 1);
                triangles[triangleIndex + 2] = (uint)(index + 0);
                triangles[triangleIndex + 3] = (uint)(index + 0);
                triangles[triangleIndex + 4] = (uint)(index + 3);
                triangles[triangleIndex + 5] = (uint)(index + 2);

                normals[index + 0] = new float3(0, 0, 1);
                normals[index + 1] = new float3(0, 0, 1);
                normals[index + 2] = new float3(0, 0, 1);
                normals[index + 3] = new float3(0, 0, 1);

                uvs[index + 0] = new float2((x + 0) * tileSize.x, (y + 0) * tileSize.y);
                uvs[index + 1] = new float2((x + 1) * tileSize.x, (y + 0) * tileSize.y);
                uvs[index + 2] = new float2((x + 1) * tileSize.x, (y + 1) * tileSize.y);
                uvs[index + 3] = new float2((x + 0) * tileSize.x, (y + 1) * tileSize.y);

                triangleIndex += 6;
                index += 4;
            }

            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetIndices(triangles, MeshTopology.Triangles, 0);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.MarkDynamic();
            mesh.name = $"Grid_{gridSize.x}x{gridSize.y}";

            vertices.Dispose();
            triangles.Dispose();
            normals.Dispose();
            uvs.Dispose();
        }

        #region Material

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Props
        {
            _position_tex,
            _normal_tex,
            _external_force_tex
        }

        #endregion
    }
}