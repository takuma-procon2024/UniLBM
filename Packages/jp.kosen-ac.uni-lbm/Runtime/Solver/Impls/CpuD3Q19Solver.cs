using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Solver.Impls
{
    public class CpuD3Q19Solver
    {
        private const uint Q = 19;
        private const uint FluidType = 0;
        private const uint BoundaryType = 1;
        private const float S = 1f / 18f; // 隣接方向
        private const float L = 1f / 36f; // 対角方向
        private const float Z = 1f / 3f; // 中心点

        private readonly uint _cellSize;

        private readonly int3[] _ei =
        {
            new(+1, +0, +0), new(-1, +0, +0), new(+0, +0, +1), new(+0, +0, -1), // 0-3
            new(+0, -1, +0), new(+0, +1, +0), new(+1, +0, +1), new(-1, +0, -1), //  4 -  7
            new(-1, +0, +1), new(+1, +0, -1), new(+1, -1, +0), new(-1, +1, +0), //  8 - 11
            new(-1, -1, +0), new(+1, +1, +0), new(+0, -1, +1), new(+0, +1, -1), // 12 - 15
            new(+0, -1, -1), new(+0, +1, +1), new(+0, +0, +0) // 16 - 18
        };

        private readonly uint[] _field;

        private readonly int[] _inv =
        {
            1, 0, 3, 2,
            5, 4, 7, 6,
            9, 8, 11, 10,
            13, 12, 15, 14,
            17, 16, 18
        };

        private readonly float4[] _particle;
        private readonly float3[] _sourceVelocity;
        private readonly float _tau;

        private readonly float[] _w =
        {
            S, S, S, S, S, S, L, L, L, L, L, L, L, L, L, L, L, L, Z
        };

        private float[] _f0, _f1;


        public CpuD3Q19Solver(uint cellSize, float tau)
        {
            _cellSize = cellSize;
            _tau = tau;
            _f0 = new float[19 * cellSize * cellSize * cellSize];
            _f1 = new float[19 * cellSize * cellSize * cellSize];
            _particle = new float4[cellSize * cellSize * cellSize];
            _field = new uint[cellSize * cellSize * cellSize];
            _sourceVelocity = new float3[cellSize * cellSize * cellSize];

            for (var x = 0; x < cellSize; x++)
            for (var y = 0; y < cellSize; y++)
            for (var z = 0; z < cellSize; z++)
            {
                var id = new uint3((uint)x, (uint)y, (uint)z);
                Initialize(id);
            }
        }

        private uint GetIndex(in uint3 id)
        {
            return id.x + id.y * _cellSize + id.z * _cellSize * _cellSize;
        }

        private void Initialize(in uint3 id)
        {
            var idx = GetIndex(id);

            const float fBase = 10.5f;

            for (var i = 0; i < Q; i++) _f0[idx * Q + i] = fBase * _w[i];
            _particle[idx] = new float4(0, 0, 0, fBase);
            _field[idx] = FluidType;

            if (id.x == 0 || id.x == _cellSize - 1 || id.y == 0 || id.y == _cellSize - 1 || id.z == 0 ||
                id.z == _cellSize - 1)
                _field[idx] = BoundaryType;

            if (id.x == _cellSize / 2 && id.y == _cellSize / 2 && id.z == _cellSize / 2)
                _sourceVelocity[idx] = new float3(0, 0.000002f, 0);
        }

        public void Step()
        {
            for (var x = 0; x < _cellSize; x++)
            for (var y = 0; y < _cellSize; y++)
            for (var z = 0; z < _cellSize; z++)
            {
                var id = new uint3((uint)x, (uint)y, (uint)z);
                Step1(id);
            }

            for (var x = 0; x < _cellSize; x++)
            for (var y = 0; y < _cellSize; y++)
            for (var z = 0; z < _cellSize; z++)
            {
                var id = new uint3((uint)x, (uint)y, (uint)z);
                Step2(id);
            }

            (_f0, _f1) = (_f1, _f0);
        }

        [Conditional("UNITY_EDITOR")]
        public void DebugDraw()
        {
            for (var x = 0; x < _cellSize; x++)
            for (var y = 0; y < _cellSize; y++)
            for (var z = 0; z < _cellSize; z++)
            {
                var idx = GetIndex(new uint3((uint)x, (uint)y, (uint)z));
                var p = _particle[idx];
                var u = p.xyz;

                var pos = new float3(x, y, z);
                if (_field[idx] != FluidType) Debug.DrawLine(pos, pos + new float3(0, 1, 0), Color.white);

                // if (lo <= 0) continue;
                if (math.dot(u, u) <= 0) continue;
                u = math.normalize(u);

                Debug.DrawLine(pos, pos + u * 0.9f, Color.red);
                Debug.DrawLine(pos + u * 0.9f, pos + u, Color.blue);
            }
        }

        private void Step1(in uint3 id)
        {
            var idx = GetIndex(id);
            var p = _particle[idx];

            var u = p.xyz;
            var lo = p.w;
            var u2 = math.dot(u, u);

            if (_field[idx] == FluidType)
                for (var i = 0; i < Q; i++)
                {
                    var a = math.dot(_ei[i], u);
                    var b = 3f * a + 4.5f * a * a - 1.5f * u2;
                    var c = lo * (1 + b);

                    _f0[idx * Q + i] -= (_f0[idx * Q + i] - c * _w[i]) / _tau;
                }
            else
                for (var i = 0; i < Q; i++)
                    _f0[idx * Q + i] = _f0[idx * Q + _inv[i]];
        }

        private void Step2(in uint3 id)
        {
            var idx = GetIndex(id);

            for (var i = 0; i < Q; i++)
            {
                var neighbor = new int3(id) - _ei[i];
                if (neighbor.x < 0 || neighbor.x >= _cellSize ||
                    neighbor.y < 0 || neighbor.y >= _cellSize ||
                    neighbor.z < 0 || neighbor.z >= _cellSize)
                    _f1[idx * Q + i] = _f0[idx * Q + i];
                else
                    _f1[idx * Q + i] = _f0[GetIndex(new uint3(neighbor)) * Q + i];
            }

            var newLo = 0f;
            var newU = float3.zero;

            if (_field[idx] == FluidType)
            {
                for (var i = 0; i < Q; i++)
                {
                    newLo += _f1[idx * Q + i];
                    newU += new float3(_ei[i]) * _f1[idx * Q + i];
                }

                if (newLo > 0)
                    newU /= newLo;

                newU += _sourceVelocity[idx];
            }
            else
            {
                var p = _particle[idx];
                newLo = p.w;
                newU = p.xyz;
            }

            _particle[idx] = new float4(newU, newLo);
        }
    }
}