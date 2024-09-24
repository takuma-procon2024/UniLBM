using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cloth
{
    public class ClothRenderer : MonoBehaviour
    {
        private static readonly int PositionTexPropId = Shader.PropertyToID("_position_tex");
        private static readonly int NormalTexPropId = Shader.PropertyToID("_normal_tex");
        private static readonly int ExternalForceTexPropId = Shader.PropertyToID("_external_force_tex");

        [SerializeField] private Material material;

        public void InitializeClothRenderer(ClothSimulationBehaviour simulation)
        {
            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            var meshFilter = gameObject.AddComponent<MeshFilter>();

            meshRenderer.material = material;
            material.SetTexture(PositionTexPropId, simulation.PositionBuffer);
            material.SetTexture(NormalTexPropId, simulation.NormalBuffer);
            material.SetTexture(ExternalForceTexPropId, simulation.InputForceBuffer);

            meshFilter.mesh = new Mesh();
            GenerateMesh(meshFilter.mesh, meshRenderer, simulation);
        }

        private static void GenerateMesh(Mesh mesh, MeshRenderer renderer, ClothSimulationBehaviour simulation)
        {
            var clothRes = simulation.ClothResolution;
            var gridSize = clothRes - 1;

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var tileSize = 1f / new float2(gridSize);

            var vertCnt = (int)(clothRes.x * clothRes.y * 4);
            var trianglesCnt = (int)(clothRes.x * clothRes.y * 6);

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

            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

            mesh.RecalculateNormals();
            // mesh.RecalculateBounds();
            mesh.MarkDynamic();
            mesh.name = $"Grid_{gridSize.x}x{gridSize.y}";

            vertices.Dispose();
            triangles.Dispose();
            normals.Dispose();
            uvs.Dispose();
        }
    }
}