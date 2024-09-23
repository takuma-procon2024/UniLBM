using System;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Lbm.Impls
{
    public class CpuD3Q15Solver : MonoBehaviour
    {
        private const uint FluidType = 0, BoundaryType = 1;

        [SerializeField] private uint width = 50;
        [SerializeField] private uint height = 50;
        [SerializeField] private uint depth = 50;
        [SerializeField] private float3 force = new(0.0002f, 0, 0);

        // f0: current density distribution, f1: next density distribution
        private float[] _f0, _f1;
        private uint[] _field;
        private float3[] _forceSource;
        private float3[] _velocity;

        private void Start()
        {
            _field = new uint[width * height * depth];
            _velocity = new float3[width * height * depth];
            _f0 = new float[Q * width * height * depth];
            _f1 = new float[Q * width * height * depth];
            _forceSource = new float3[width * height * depth];

            Array.Fill(_field, FluidType);

            // var random = new Random(231576);
            // PutDebugObstacles(ref random);

            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
            for (var z = 0; z < depth; z++)
            {
                var idx = x + y * width + z * width * height;
                for (var k = 0; k < Q; k++) _f0[k + idx * Q] = _w[k];
            }
        }

        private void Update()
        {
            AddDebugForce();

            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
            for (var z = 0; z < depth; z++)
                Eval(new int3(x, y, z));

            // swap f0 and f1
            (_f0, _f1) = (_f1, _f0);

            VelocityDebugDraw();
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Draw debug obstacles
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
            for (var z = 0; z < depth; z++)
            {
                var idx = x + y * width + z * width * height;
                if (_field[idx] != BoundaryType) continue;

                var pos = new Vector3(x, y, z);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(pos, Vector3.one);
            }
        }

        private void PutDebugObstacles(ref Random random)
        {
            var radius = (int)(height / 4);
            for (var i = 0; i < 2; i++)
            {
                var px = (int)(random.NextFloat() * width);
                var py = (int)(random.NextFloat() * height);
                var pz = (int)(random.NextFloat() * depth);
                var center = new int3(px, py, pz);

                for (var x = px - radius; x < px + radius; x++)
                for (var y = py - radius; y < py + radius; y++)
                for (var z = pz - radius; z < pz + radius; z++)
                {
                    var p = new int3(x, y, z);
                    if (math.lengthsq(p - center) >= radius * radius) continue;

                    var k = Per(p, new int3((int)width - 1, (int)height - 1, (int)depth - 1));
                    var idx = k.x + k.y * width + k.z * width * height;
                    _field[idx] = BoundaryType;
                }
            }
        }

        private void VelocityDebugDraw()
        {
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
            for (var z = 0; z < depth; z++)
            {
                var idx = x + y * width + z * width * height;
                var pos = new Vector3(x, y, z);
                var dir = new Vector3(_velocity[idx].x, _velocity[idx].y, _velocity[idx].z);
                var dirLength = math.length(dir);
                if (dirLength < 0.0005f) continue;

                dir /= dirLength;
                dir *= 0.9f;

                // 先端の色を変える
                Debug.DrawRay(pos, dir * 0.9f, Color.red);
                Debug.DrawRay(pos + dir * 0.9f, dir * 0.1f, Color.blue);
            }
        }

        private static int Per(int idx, int maxIdx)
        {
            if (idx < 0) return idx + maxIdx + 1;
            if (idx > maxIdx) return idx - (maxIdx + 1);
            return idx;
        }

        private static int3 Per(in int3 idx, in int3 maxIdx)
        {
            return new int3(Per(idx.x, maxIdx.x), Per(idx.y, maxIdx.y), Per(idx.z, maxIdx.z));
        }

        private void AddDebugForce()
        {
            // for (var x = 0; x < width; x++)
            // for (var y = 0; y < height; y++)
            // for (var z = 0; z < depth; z++)
            // {
            //     var idx = x + y * width + z * width * height;
            //     if (idx % width == 0)
            //         _forceSource[idx] = force;
            //     if (idx % width == width - 1)
            //         _forceSource[idx] = -force;
            // }

            _forceSource[10 + 10 * width + 10 * width * height] = force;
        }

        private void Eval(in int3 pos)
        {
            var idx = pos.x + pos.y * width + pos.z * width * height;
            if (_field[idx] != FluidType) return;

            // calculate density and velocity
            float rho = 0, ux = 0, uy = 0, uz = 0;
            for (var m = 0; m < Q; m++)
            {
                rho += _f0[m + idx * Q];
                ux += _ex[m] * _f0[m + idx * Q];
                uy += _ey[m] * _f0[m + idx * Q];
                uz += _ez[m] * _f0[m + idx * Q];
            }

            ux /= rho;
            uy /= rho;
            uz /= rho;

            // store velocity;
            _velocity[idx] = new float3(ux, uy, uz);

            // update velocity with force source
            ux += _forceSource[idx].x * 0.5f;
            uy += _forceSource[idx].y * 0.5f;
            uz += _forceSource[idx].z * 0.5f;

            // collision + streaming
            for (var m = 0; m < Q; m++)
            {
                var p = pos + new int3(_ex[m], _ey[m], _ez[m]);
                p = Per(p, new int3((int)width - 1, (int)height - 1, (int)depth - 1));
                var idxP = p.x + p.y * width + p.z * width * height;

                var newF = (1 - Omega) * _f0[idx * Q + m] + Omega * _w[m] * rho *
                    (1f - 3f / 2f * (ux * ux + uy * uy + uz * uz) +
                     3f * (_ex[m] * ux + _ey[m] * uy + _ez[m] * uz) + 9f / 2f *
                     (_ex[m] * ux + _ey[m] * uy + _ez[m] * uz) * (_ex[m] * ux + _ey[m] * uy + _ez[m] * uz));

                if (_field[idxP] == BoundaryType) _f1[idx * Q + _inv[m]] = newF;
                else _f1[idxP * Q + m] = newF;
            }
        }

        #region Constants for LBM

        private const float Tau = 0.6f;
        private const float Omega = 1.0f / Tau;

        private const int Q = 15;
        private const float L = 1.0f / 72.0f;
        private const float S = 1.0f / 9.0f;
        private const float Z = 2.0f / 9.0f;
        private readonly int[] _ex = { 0, -1, 0, 0, -1, -1, -1, -1, 1, 0, 0, 1, 1, 1, 1 };
        private readonly int[] _ey = { 0, 0, -1, 0, -1, -1, 1, 1, 0, 1, 0, 1, 1, -1, -1 };
        private readonly int[] _ez = { 0, 0, 0, -1, -1, 1, -1, 1, 0, 0, 1, 1, -1, 1, -1 };
        private readonly int[] _inv = { 0, 8, 9, 10, 11, 12, 13, 14, 1, 2, 3, 4, 5, 6, 7 };

        private readonly float[] _w =
        {
            Z, S, S, S, L, L, L, L, S, S, S, L, L, L, L
        };

        #endregion
    }
}