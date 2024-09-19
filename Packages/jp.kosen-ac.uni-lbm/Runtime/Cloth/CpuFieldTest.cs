using System;
using Solver;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Cloth
{
    public class CpuFieldTest : MonoBehaviour
    {
        [SerializeField] private ClothSimulationBehaviour cloth;
        [SerializeField] private LbmSolverBehaviour lbmSolver;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float3 offset, scale = 1;
        
        private uint[] _obstacleMap;
        private Texture2D _posTex;
        private bool _isInitialized;
        private NativeArray<float4> _posBuffer;

        private void OnGUI()
        {
            if (GUILayout.Button("Calc Obstacles"))
                CalcObstacles();
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !_isInitialized) return;
            
            var cellRes = lbmSolver.Solver.GetCellSize();
            var center = new float3(cellRes / 2f, cellRes / 2f, cellRes / 2f) * cellSize;
            var size = new Vector3(cellRes, cellRes, cellRes) * cellSize;
            Gizmos.DrawWireCube(center, size);
            
            var clothRes = cloth.ClothResolution;
            for (var x = 0; x < clothRes.x; x++)
            for (var y = 0; y < clothRes.y; y++)
            {
                var pos = _posBuffer[(int)(x + y * clothRes.x)].xyz;
                pos *= scale;
                pos += offset;
                Gizmos.DrawWireSphere(pos * cellSize, 0.1f);
            }
            
            for (var x = 0; x < cellRes; x++)
            for (var y = 0; y < cellRes; y++)
            for (var z = 0; z < cellRes; z++)
            {
                var idx = z * cellRes * cellRes + y * cellRes + x;
                if (_obstacleMap[idx] != 1) continue;
                var pos = new float3(x, y, z) * cellSize;
                Gizmos.DrawWireCube(pos, new Vector3(cellSize, cellSize, cellSize));
            }
        }

        private void CalcObstacles()
        {
            var posTex = cloth.PositionBuffer;
            var cellRes = lbmSolver.Solver.GetCellSize();
            if (_posTex == null)
                _posTex = new Texture2D(posTex.width, posTex.height, TextureFormat.RGBAFloat, false);
            _obstacleMap ??= new uint[cellRes * cellRes * cellRes];

            RenderTexture.active = posTex;
            _posTex.ReadPixels(new Rect(0, 0, posTex.width, posTex.height), 0, 0);
            _posTex.Apply();
            RenderTexture.active = null;

            _posBuffer = _posTex.GetPixelData<float4>(0);

            Array.Clear(_obstacleMap, 0, _obstacleMap.Length);
            for (var x = 0; x < posTex.width / 4; x++)
            for (var y = 0; y < posTex.height / 4; y++)
                Kernel(new uint2((uint)x, (uint)y), _posBuffer);
            
            _isInitialized = true;
        }

        private void Kernel(in uint2 id, in NativeArray<float4> posBuffer)
        {
            var pos00 = posBuffer[GetIdx(id * 4 + new uint2(0, 0))].xyz;
            var pos01 = posBuffer[GetIdx(id * 4 + new uint2(0, 1))].xyz;
            var pos10 = posBuffer[GetIdx(id * 4 + new uint2(1, 0))].xyz;
            var pos11 = posBuffer[GetIdx(id * 4 + new uint2(1, 1))].xyz;

            var min = math.min(math.min(pos00, pos01), math.min(pos10, pos11));
            var max = math.max(math.max(pos00, pos01), math.max(pos10, pos11));
            min *= scale;
            max *= scale;
            min += offset;
            max += offset;
            
            max += cellSize;

            var cellRes = lbmSolver.Solver.GetCellSize();
            for (var x = min.x; x < max.x; x += cellSize)
            for (var y = min.y; y < max.y; y += cellSize)
            for (var z = min.z; z < max.z; z += cellSize)
            {
                var cellIdFloat = new float3(x, y, z) / cellSize;
                var cellId = new uint3(math.floor(cellIdFloat));
                if (cellId.x >= cellRes || cellId.y >= cellRes || cellId.z >= cellRes) continue;

                var idx = cellId.z * cellRes * cellRes + cellId.y * cellRes + cellId.x;
                if (idx >= _obstacleMap.Length) continue;
                _obstacleMap[idx] = 1;
            }

            return;

            int GetIdx(in uint2 idx)
            {
                return (int)(idx.y * cloth.ClothResolution.x + idx.x);
            }
        }
    }
}