using System;
using UniLbm.Lbm;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Cloth
{
    public class CpuFieldTest
    {
        private const float CellSize = 1f;

        private readonly ClothSolver _clothSolver;
        private readonly ILbmSolver _lbmSolver;
        private readonly Transform _transform;


        private bool _isInitialized;
        private uint[] _obstacleMap;
        private NativeArray<float4> _posBuffer;
        private Texture2D _posTex;

        public CpuFieldTest(Transform transform, ClothSolver clothSolver, ILbmSolver lbmSolver)
        {
            _transform = transform;
            _clothSolver = clothSolver;
            _lbmSolver = lbmSolver;
        }

        public void OnGUI()
        {
            if (GUILayout.Button("Calc Obstacles"))
                CalcObstacles();
        }

        public void OnDrawGizmos()
        {
            if (!Application.isPlaying || !_isInitialized) return;

            var cellRes = _lbmSolver.CellRes;
            var center = new float3(cellRes / 2f, cellRes / 2f, cellRes / 2f) * CellSize;
            var size = new Vector3(cellRes, cellRes, cellRes) * CellSize;
            Gizmos.DrawWireCube(center, size);

            var trs = _transform.localToWorldMatrix;
            var clothRes = _clothSolver.ClothResolution;
            for (var x = 0; x < clothRes.x; x++)
            for (var y = 0; y < clothRes.y; y++)
            {
                var pos = _posBuffer[x + y * clothRes.x].xyz;
                pos = trs.MultiplyPoint(pos);
                Gizmos.DrawWireSphere(pos * CellSize, 0.1f);
            }

            for (var x = 0; x < cellRes; x++)
            for (var y = 0; y < cellRes; y++)
            for (var z = 0; z < cellRes; z++)
            {
                var idx = z * cellRes * cellRes + y * cellRes + x;
                if (_obstacleMap[idx] != 1) continue;
                var pos = new float3(x, y, z) * CellSize;
                pos -= 0.5f * CellSize;
                Gizmos.DrawWireCube(pos, new Vector3(CellSize, CellSize, CellSize));
            }
        }

        private void CalcObstacles()
        {
            var posTex = _clothSolver.PositionBuffer;
            var cellRes = _lbmSolver.CellRes;
            if (_posTex == null)
                _posTex = new Texture2D(posTex.width, posTex.height, TextureFormat.RGBAFloat, false);
            _obstacleMap ??= new uint[cellRes * cellRes * cellRes];

            RenderTexture.active = posTex;
            _posTex.ReadPixels(new Rect(0, 0, posTex.width, posTex.height), 0, 0);
            _posTex.Apply();
            RenderTexture.active = null;

            _posBuffer = _posTex.GetPixelData<float4>(0);

            var trs = _transform.localToWorldMatrix;
            Array.Clear(_obstacleMap, 0, _obstacleMap.Length);
            for (var x = 0; x < posTex.width - 1; x++)
            for (var y = 0; y < posTex.height - 1; y++)
                Kernel(new uint2((uint)x, (uint)y), _posBuffer, trs);

            _isInitialized = true;
        }

        private void Kernel(in uint2 quadId, in NativeArray<float4> posBuffer, float4x4 trs)
        {
            var pos00 = posBuffer[GetIdx(quadId + new uint2(0, 0))].xyz;
            var pos01 = posBuffer[GetIdx(quadId + new uint2(0, 1))].xyz;
            var pos10 = posBuffer[GetIdx(quadId + new uint2(1, 0))].xyz;
            var pos11 = posBuffer[GetIdx(quadId + new uint2(1, 1))].xyz;

            var min = math.min(math.min(pos00, pos01), math.min(pos10, pos11));
            var max = math.max(math.max(pos00, pos01), math.max(pos10, pos11));

            min = math.mul(trs, new float4(min, 1)).xyz;
            max = math.mul(trs, new float4(max, 1)).xyz;

            var cellRes = _lbmSolver.CellRes;
            for (var x = min.x; x <= max.x; x += CellSize)
            for (var y = min.y; y <= max.y; y += CellSize)
            for (var z = min.z; z <= max.z; z += CellSize)
            {
                var cellIdFloat = new float3(x, y, z) / CellSize;
                var cellId = new uint3(math.ceil(cellIdFloat));
                if (cellId.x >= cellRes || cellId.y >= cellRes || cellId.z >= cellRes) continue;

                var idx = cellId.z * cellRes * cellRes + cellId.y * cellRes + cellId.x;
                if (idx >= _obstacleMap.Length) continue;
                _obstacleMap[idx] = 1;
            }

            return;

            int GetIdx(in uint2 idx)
            {
                return (int)(idx.y * _clothSolver.ClothResolution.x + idx.x);
            }
        }
    }
}