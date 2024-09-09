using Unity.Mathematics;
using UnityEngine;

namespace Solver
{
    public class LbmCpuSolver3D : MonoBehaviour
    {
        [SerializeField] private uint3 size;
        [SerializeField] private float velocityCoef;
        [SerializeField] private float visc;
        [SerializeField] private float3 force;

        private LbmSpuSolver3DImpl _solver;

        private void Start()
        {
            _solver = new LbmSpuSolver3DImpl(size, velocityCoef, visc);
        }

        private void Update()
        {
            _solver.AddDebugForce(force);
            _solver.Update();
            _solver.VelocityDebugDraw();
        }
    }

    public class LbmSpuSolver3DImpl
    {
        public LbmSpuSolver3DImpl(in uint3 size, float velocityCoef, float visc)
        {
            _width = size.x;
            _height = size.y;
            _depth = size.z;

            _velocityCoef = velocityCoef;
            _visc = visc;

            _prev = new Float3Array(size);
            _velocity = new Float3Array(size);
            _velocitySource = new Float3Array(size);
        }

        public void Update()
        {
            for (uint z = 0; z < _depth; z++)
            for (uint y = 0; y < _height; y++)
            for (uint x = 0; x < _width; x++)
            {
                var id = new uint3(x, y, z);

                AddSourceVelocity(in id, Time.deltaTime);

                DiffuseVelocity(in id, Time.deltaTime);

                ProjectStep1(in id);
                ProjectStep2(in id);
                ProjectStep3(in id);

                SwapVelocity(in id);

                AdvectVelocity(in id, Time.deltaTime);

                ProjectStep1(in id);
                ProjectStep2(in id);
                ProjectStep3(in id);
            }
        }

        public void VelocityDebugDraw()
        {
            for (var x = 0; x < _width; x++)
            for (var y = 0; y < _height; y++)
            for (var z = 0; z < _depth; z++)
            {
                var idx = x + y * _width + z * _width * _height;
                var pos = new Vector3(x, y, z);
                var dir = new Vector3(_velocity[idx].x, _velocity[idx].y, _velocity[idx].z);
                if (dir == Vector3.zero) dir = Vector3.up;
                dir.Normalize();
                dir *= 0.9f;

                // 先端の色を変える
                Debug.DrawRay(pos, dir * 0.9f, Color.red);
                Debug.DrawRay(pos + dir * 0.9f, dir * 0.1f, Color.blue);
            }
        }

        public void AddDebugForce(float3 force)
        {
            for (var x = 0; x < _width; x++)
            for (var y = 0; y < _height; y++)
            for (var z = 0; z < _depth; z++)
                if (x == 0)
                    _velocitySource[x + y * _width + z * _width * _height] = force;
        }

        #region Utility structs

        private readonly struct Float3Array
        {
            private readonly uint3 _size;
            private readonly float3[] _data;

            private uint PerIndex(in uint3 id)
            {
                var x = id.x >= _size.x ? id.x - _size.x : id.x;
                var y = id.y >= _size.y ? id.y - _size.y : id.y;
                var z = id.z >= _size.z ? id.z - _size.z : id.z;
                return x + y * _size.x + z * _size.x * _size.y;
            }

            private uint PerIndex(in int3 id)
            {
                var x = id.x < 0 ? id.x + _size.x : id.x >= _size.x ? id.x - _size.x : id.x;
                var y = id.y < 0 ? id.y + _size.y : id.y >= _size.y ? id.y - _size.y : id.y;
                var z = id.z < 0 ? id.z + _size.z : id.z >= _size.z ? id.z - _size.z : id.z;
                return (uint)(x + y * _size.x + z * _size.x * _size.y);
            }

            public float3 this[in uint3 id]
            {
                get => _data[PerIndex(in id)];
                set => _data[PerIndex(in id)] = value;
            }

            public float3 this[in int3 id]
            {
                get => _data[PerIndex(in id)];
                set => _data[PerIndex(in id)] = value;
            }

            public float3 this[long id]
            {
                get => _data[id];
                set => _data[id] = value;
            }

            public Float3Array(in uint3 size)
            {
                _size = size;
                _data = new float3[size.x * size.y * size.z];
            }
        }

        #endregion

        #region LBM Steps

        /// <summary>
        ///     速度場外力項
        /// </summary>
        private void AddSourceVelocity(in uint3 id, float deltaTIme)
        {
            if (id.x >= _width || id.y >= _height || id.z >= _depth) return;

            _velocity[id] += _velocitySource[id] * _velocityCoef * deltaTIme;
            _prev[id] = _velocity[id];
        }

        /// <summary>
        ///     速度場拡散項
        /// </summary>
        private void DiffuseVelocity(in uint3 id, float deltaTime)
        {
            if (id.x >= _width || id.y >= _height || id.z >= _depth) return;

            var a = deltaTime * _visc * _width * _height;

            for (var k = 0; k < GsIterate; k++)
            {
                _velocity[id] = (_prev[id].xyz + a *
                    (_velocity[new int3((int)id.x - 1, (int)id.y, (int)id.z)] +
                     _velocity[new uint3(id.x + 1, id.yz)] +
                     _velocity[new int3((int)id.x, (int)id.y - 1, (int)id.z)] +
                     _velocity[new uint3(id.x, id.y + 1, id.z)] +
                     _velocity[new int3((int)id.x, (int)id.y, (int)id.z - 1)] +
                     _velocity[new uint3(id.xy, id.z + 1)])) / (1 + 6 * a);
                SetBoundaryVelocity(in id, _width, _height, _depth);
            }
        }

        /// <summary>
        ///     速度場移流項
        /// </summary>
        private void AdvectVelocity(in uint3 id, float deltaTime)
        {
            if (id.x >= _width || id.y >= _height || id.x >= _depth) return;

            var dfdt = deltaTime * (_width + _height + _depth) / 3;

            //バックトレースポイント割り出し.
            var x = id.x - dfdt * _prev[id].x;
            var y = id.y - dfdt * _prev[id].y;
            var z = id.z - dfdt * _prev[id].z;
            //ポイントがシミュレーション範囲内に収まるようにクランプ.
            x = math.clamp(x, 0.5f, _width - 1.5f);
            y = math.clamp(y, 0.5f, _height - 1.5f);
            z = math.clamp(z, 0.5f, _depth - 1.5f);
            //xyzのそれぞれ近似の偏微分セルを求める.
            var ddx0 = (uint)math.floor(x);
            var ddx1 = ddx0 + 1;
            var ddy0 = (uint)math.floor(y);
            var ddy1 = ddy0 + 1;
            var ddz0 = (uint)math.floor(z);
            var ddz1 = ddz0 + 1;
            //近傍セルとの線形補間用の差分を取っておく.
            var s1 = x - ddx0;
            var s0 = 1.0f - s1;
            var t1 = y - ddy0;
            var t0 = 1.0f - t1;
            var u1 = z - ddz0;
            var u0 = 1.0f - u1;

            //バックトレースし、1step前の値を近傍との線形補間をとって、現在の速度場に代入。
            _velocity[id] =
                s0 * u0 *
                (t0 * _prev[new uint3(ddx0, ddy0, ddz0)].xyz + t1 * _prev[new uint3(ddx0, ddy1, ddz0)].xyz) +
                s1 * u0 *
                (t0 * _prev[new uint3(ddx1, ddy0, ddz0)].xyz + t1 * _prev[new uint3(ddx1, ddy1, ddz0)].xyz) +
                s0 * u1 *
                (t0 * _prev[new uint3(ddx0, ddy0, ddz1)].xyz + t1 * _prev[new uint3(ddx0, ddy1, ddz1)].xyz) +
                s1 * u1 *
                (t0 * _prev[new uint3(ddx1, ddy0, ddz1)].xyz + t1 * _prev[new uint3(ddx1, ddy1, ddz1)].xyz);
            SetBoundaryVelocity(id, _width, _height, _depth);
        }

        private void SwapVelocity(in uint3 id)
        {
            if (id.x >= _width || id.y >= _height || id.z >= _depth) return;

            (_velocity[id], _prev[id]) = (_prev[id], _velocity[id]);
        }

        /// <summary>
        ///     質量保存ステップ1
        /// </summary>
        private void ProjectStep1(in uint3 id)
        {
            if (id.x >= _width || id.y >= _height || id.z >= _depth) return;

            var uvd = new float3(1.0f / _width, 1.0f / _height, 1.0f / _depth);
            _prev[id] = new float3(0f,
                -0.5f * (uvd.x * (_velocity[new uint3(id.x + 1, id.y, id.z)].x -
                                  _velocity[new int3((int)id.x - 1, (int)id.y, (int)id.z)].x)) +
                uvd.y * (_velocity[new uint3(id.x, id.y + 1, id.z)].y -
                         _velocity[new int3((int)id.x, (int)id.y - 1, (int)id.z)].y) +
                uvd.z * (_velocity[new uint3(id.x, id.y, id.z + 1)].z -
                         _velocity[new int3((int)id.x, (int)id.y, (int)id.z - 1)].z),
                _prev[id].z);

            SetBoundaryDiv(in id, _width, _height, _depth);
            SetBoundaryPrev(in id, _width, _height, _depth);
        }

        /// <summary>
        ///     質量保存ステップ2
        /// </summary>
        private void ProjectStep2(in uint3 id)
        {
            if (id.x >= _width || id.y >= _height || id.z >= _depth) return;

            for (var k = 0; k < GsIterate; k++)
            {
                _prev[id] = new float3(
                    (_prev[id].y +
                     _prev[new int3((int)id.x - 1, (int)id.y, (int)id.z)].x +
                     _prev[new uint3(id.x + 1, id.y, id.z)].x +
                     _prev[new int3((int)id.x, (int)id.y - 1, (int)id.z)].x +
                     _prev[new uint3(id.x, id.y + 1, id.z)].x +
                     _prev[new int3((int)id.x, (int)id.y, (int)id.z - 1)].x +
                     _prev[new uint3(id.x, id.y, id.z + 1)].x) / 6,
                    _prev[id].yz);
                SetBoundaryPrev(id, _width, _height, _depth);
            }
        }

        private void ProjectStep3(in uint3 id)
        {
            if (id.x >= _width || id.y >= _height || id.z >= _depth) return;

            var uvd = new float3(1f / _width, 1f / _height, 1f / _depth);

            var velX = _velocity[id].x;
            var velY = _velocity[id].y;
            var velZ = _velocity[id].z;

            velX -= 0.5f * (_prev[new uint3(id.x + 1, id.y, id.z)].x -
                            _prev[new int3((int)id.x - 1, (int)id.y, (int)id.z)].x) /
                    uvd.x;
            velY -= 0.5f * (_prev[new uint3(id.x, id.y + 1, id.z)].x -
                            _prev[new int3((int)id.x, (int)id.y - 1, (int)id.z)].x) /
                    uvd.y;
            velZ -= 0.5f * (_prev[new uint3(id.x, id.y, id.z + 1)].x -
                            _prev[new int3((int)id.x, (int)id.y, (int)id.z - 1)].x) /
                    uvd.z;

            _velocity[id] = new float3(velX, velY, velZ);
            SetBoundaryVelocity(id, _width, _height, _depth);
        }

        #endregion

        #region Variables

        private readonly Float3Array _prev;
        private readonly Float3Array _velocity;
        private readonly Float3Array _velocitySource;

        private readonly uint _width, _height, _depth;
        private readonly float _velocityCoef, _visc;

        private const int GsIterate = 2;

        #endregion

        #region Utilities

        private void SetBoundaryVelocity(in uint3 id, in uint w, in uint h, in uint d)
        {
            _velocity[id] = id.x == 0
                ? new float3(-_velocity[id + new uint3(1, 0, 0)].x, _velocity[id].yz)
                : _velocity[id];
            _velocity[id] = id.x == w - 1
                ? new float3(-_velocity[new uint3(w - 2, id.yz)].x, _velocity[id].yz)
                : _velocity[id];
            _velocity[id] = id.y == 0
                ? new float3(_velocity[id].x, -_velocity[id + new uint3(0, 1, 0)].y, _velocity[id].z)
                : _velocity[id];
            _velocity[id] = id.y == h - 1
                ? new float3(_velocity[id].x, -_velocity[new uint3(id.x, h - 2, id.z)].y, _velocity[id].z)
                : _velocity[id];
            _velocity[id] = id.z == 0
                ? new float3(_velocity[id].xy, -_velocity[id + new uint3(0, 0, 1)].z)
                : _velocity[id];
            _velocity[id] = id.z == d - 1
                ? new float3(_velocity[id].xy, -_velocity[new uint3(id.xy, d - 2)].z)
                : _velocity[id];

            _velocity[id] = id is { x: 0, y: 0 } && id.z != 0 && id.z != d - 1
                ? 0.5f * (_velocity[new uint3(1, 0, id.z)] + _velocity[new uint3(0, 1, id.z)])
                : _velocity[id];
            _velocity[id] = id is { x: 0, y: 0, z: 0 }
                ? (_velocity[new uint3(1, 0, 0)] + _velocity[new uint3(0, 1, 0)] + _velocity[new uint3(0, 0, 1)]) / 3
                : _velocity[id];
            _velocity[id] = id is { x: 0, y: 0 } && id.z == d - 1
                ? (_velocity[new uint3(1, 0, d - 1)] + _velocity[new uint3(0, 1, d - 1)] +
                   _velocity[new uint3(0, 0, d - 2)]) / 3
                : _velocity[id];
            _velocity[id] = id.x == 0 && id.y == h - 1 && id.z != 0 && id.z != d - 1
                ? 0.5f * (_velocity[new uint3(1, h - 1, id.z)] + _velocity[new uint3(0, h - 2, id.z)])
                : _velocity[id];
            _velocity[id] = id.x == 0 && id.y == h - 1 && id.z == 0
                ? (_velocity[new uint3(1, h - 1, 0)] + _velocity[new uint3(0, h - 2, 0)] +
                   _velocity[new uint3(0, h - 1, 1)]) / 3
                : _velocity[id];
            _velocity[id] = id.x == 0 && id.y == h - 1 && id.z == d - 1
                ? (_velocity[new uint3(1, h - 1, d - 1)] + _velocity[new uint3(0, h - 2, d - 1)] +
                   _velocity[new uint3(0, h - 1, d - 2)]) / 3
                : _velocity[id];
            _velocity[id] = id.x == w - 1 && id.y == 0 && id.z != 0 && id.z != d - 1
                ? 0.5f * (_velocity[new uint3(w - 2, 0, id.z)] + _velocity[new uint3(w - 1, 1, id.z)])
                : _velocity[id];
            _velocity[id] = id.x == w - 1 && id is { y: 0, z: 0 }
                ? (_velocity[new uint3(w - 2, 0, 0)] + _velocity[new uint3(w - 1, 1, 0)] +
                   _velocity[new uint3(w - 1, 0, 1)]) / 3
                : _velocity[id];
            _velocity[id] = id.x == w - 1 && id.y == 0 && id.z == d - 1
                ? (_velocity[new uint3(w - 2, 0, d - 1)] + _velocity[new uint3(w - 1, 1, d - 1)] +
                   _velocity[new uint3(w - 1, 0, d - 2)]) / 3
                : _velocity[id];
            _velocity[id] = id.x == w - 1 && id.y == h - 1 && id.z != 0 && id.z != d - 1
                ? 0.5f * (_velocity[new uint3(w - 2, h - 1, id.z)] + _velocity[new uint3(w - 1, h - 2, id.z)])
                : _velocity[id];
            _velocity[id] = id.x == w - 1 && id.y == h - 1 && id.z == 0
                ? (_velocity[new uint3(w - 2, h - 1, 0)] + _velocity[new uint3(w - 1, h - 2, 0)] +
                   _velocity[new uint3(w - 1, h - 1, 1)]) / 3
                : _velocity[id];
            _velocity[id] = id.x == w - 1 && id.y == h - 1 && id.z != d - 1
                ? (_velocity[new uint3(w - 2, h - 1, d - 1)] + _velocity[new uint3(w - 1, h - 2, d - 1)] +
                   _velocity[new uint3(w - 1, h - 1, d - 2)]) / 3
                : _velocity[id];
        }

        private void SetBoundaryDiv(in uint3 id, in uint w, in uint h, in uint d)
        {
            _prev[id] = id.x == 0 ? new float3(_prev[id].x, _prev[id + new uint3(1, 0, 0)].y, _prev[id].z) : _prev[id];
            _prev[id] = id.x == w - 1
                ? new float3(_prev[id].x, _prev[new uint3(w - 2, id.yz)].y, _prev[id].z)
                : _prev[id];
            _prev[id] = id.y == 0 ? new float3(_prev[id].x, _prev[id + new uint3(0, 1, 0)].y, _prev[id].z) : _prev[id];
            _prev[id] = id.y == h - 1
                ? new float3(_prev[id].x, _prev[new uint3(id.x, h - 2, id.z)].y, _prev[id].z)
                : _prev[id];
            _prev[id] = id.z == 0 ? new float3(_prev[id].x, _prev[id + new uint3(0, 0, 1)].y, _prev[id].z) : _prev[id];
            _prev[id] = id.z == d - 1
                ? new float3(_prev[id].x, _prev[new uint3(id.xy, d - 2)].y, _prev[id].z)
                : _prev[id];

            _prev[id] = id is { x: 0, y: 0 } && id.z != 0 && id.z != d - 1
                ? new float3(_prev[id].x, 0.5f * (_prev[new uint3(1, 0, id.z)].y + _prev[new uint3(0, 1, id.z)].y),
                    _prev[id].z)
                : _prev[id];
            _prev[id] = id.x == 0 && id is { y: 0, z: 0 }
                ? new float3(_prev[id].x,
                    (_prev[new uint3(1, 0, 0)].y + _prev[new uint3(0, 1, 0)].y + _prev[new uint3(0, 0, 1)].y) / 3,
                    _prev[id].z)
                : _prev[id];
            _prev[id] = id is { x: 0, y: 0 } && id.z == d - 1
                ? new float3(_prev[id].x,
                    (_prev[new uint3(1, 0, d - 1)].y + _prev[new uint3(0, 1, d - 1)].y +
                     _prev[new uint3(0, 0, d - 2)].y) / 3, _prev[id].z)
                : _prev[id];
            _prev[id] = id.x == 0 && id.y == h - 1 && id.z != 0 && id.z != d - 1
                ? new float3(_prev[id].x,
                    0.5f * (_prev[new uint3(1, h - 1, id.z)].y + _prev[new uint3(0, h - 2, id.z)].y), _prev[id].z)
                : _prev[id];
            _prev[id] = id.x == 0 && id.y == h - 1 && id.z == 0
                ? new float3(_prev[id].x,
                    (_prev[new uint3(1, h - 1, 0)].y + _prev[new uint3(0, h - 2, 0)].y +
                     _prev[new uint3(0, h - 1, 1)].y) / 3, _prev[id].z)
                : _prev[id];
            _prev[id] = id.x == 0 && id.y == h - 1 && id.z == d - 1
                ? new float3(_prev[id].x,
                    (_prev[new uint3(1, h - 1, d - 1)].y + _prev[new uint3(0, h - 2, d - 1)].y +
                     _prev[new uint3(0, h - 1, d - 2)].y) / 3, _prev[id].z)
                : _prev[id];
            _prev[id] = id.x == w - 1 && id.y == 0 && id.z != 0 && id.z != d - 1
                ? new float3(_prev[id].x,
                    0.5f * (_prev[new uint3(w - 2, 0, id.z)].y + _prev[new uint3(w - 1, 1, id.z)].y), _prev[id].z)
                : _prev[id];
            _prev[id] = id.x != w - 1 || id.y != 0 || id.z != 0
                ? new float3(_prev[id].x,
                    (_prev[new uint3(w - 2, 0, 0)].y + _prev[new uint3(w - 1, 1, 0)].y +
                     _prev[new uint3(w - 1, 0, 1)].y) / 3, _prev[id].z)
                : _prev[id];
            _prev[id] = id.x == w - 1 && id.y == 0 && id.z == d - 1
                ? new float3(_prev[id].x,
                    (_prev[new uint3(w - 2, 0, d - 1)].y + _prev[new uint3(w - 1, 1, d - 1)].y +
                     _prev[new uint3(w - 1, 0, d - 2)].y) / 3, _prev[id].z)
                : _prev[id];
            _prev[id] = id.x == w - 1 && id.y == h - 1 && id.z != 0 && id.z != d - 1
                ? new float3(_prev[id].x,
                    0.5f * (_prev[new uint3(w - 2, h - 1, id.z)].y + _prev[new uint3(w - 1, h - 2, id.z)].y),
                    _prev[id].z)
                : _prev[id];
            _prev[id] = id.x == w - 1 && id.y == h - 1 && id.z == 0
                ? new float3(_prev[id].x,
                    (_prev[new uint3(w - 2, h - 1, 0)].y + _prev[new uint3(w - 1, h - 2, 0)].y +
                     _prev[new uint3(w - 1, h - 1, 1)].y) / 3, _prev[id].z)
                : _prev[id];
            _prev[id] = id.x == w - 1 && id.y == h - 1 && id.z != d - 1
                ? new float3(_prev[id].x,
                    (_prev[new uint3(w - 2, h - 1, d - 1)].y + _prev[new uint3(w - 1, h - 2, d - 1)].y +
                     _prev[new uint3(w - 1, h - 1, d - 2)].y) / 3, _prev[id].z)
                : _prev[id];
        }

        private void SetBoundaryPrev(in uint3 id, in uint w, in uint h, in uint d)
        {
            _prev[id] = id.x == 0 ? new float3(_prev[id + new uint3(1, 0, 0)].x, _prev[id].yz) : _prev[id];
            _prev[id] = id.x == w - 1 ? new float3(_prev[new uint3(w - 2, id.yz)].x, _prev[id].yz) : _prev[id];
            _prev[id] = id.y == 0 ? new float3(_prev[id + new uint3(0, 1, 0)].x, _prev[id].yz) : _prev[id];
            _prev[id] = id.y == h - 1 ? new float3(_prev[new uint3(id.x, h - 2, id.z)].x, _prev[id].yz) : _prev[id];
            _prev[id] = id.z == 0 ? new float3(_prev[id + new uint3(0, 0, 1)].x, _prev[id].yz) : _prev[id];
            _prev[id] = id.z == d - 1 ? new float3(_prev[new uint3(id.xy, d - 2)].x, _prev[id].yz) : _prev[id];

            _prev[id] = id is { x: 0, y: 0 } && id.z != 0 && id.z != d - 1
                ? new float3(0.5f * (_prev[new uint3(1, 0, id.z)].x + _prev[new uint3(0, 1, id.z)].x), _prev[id].yz)
                : _prev[id];
            _prev[id] = id is { x: 0, y: 0, z: 0 }
                ? new float3(
                    (_prev[new uint3(1, 0, 0)].x + _prev[new uint3(0, 1, 0)].x + _prev[new uint3(0, 0, 1)].x) / 3,
                    _prev[id].yz)
                : _prev[id];
            _prev[id] = id is { x: 0, y: 0 } && id.z == d - 1
                ? new float3(
                    (_prev[new uint3(1, 0, d - 1)].x + _prev[new uint3(0, 1, d - 1)].x +
                     _prev[new uint3(0, 0, d - 2)].x) / 3, _prev[id].yz)
                : _prev[id];
            _prev[id] = id.x == 0 && id.y == h - 1 && id.z != 0 && id.z != d - 1
                ? new float3(0.5f * (_prev[new uint3(1, h - 1, id.z)].x + _prev[new uint3(0, h - 2, id.z)].x),
                    _prev[id].yz)
                : _prev[id];
            _prev[id] = id.x == 0 && id.y == h - 1 && id.z == 0
                ? new float3(
                    (_prev[new uint3(1, h - 1, 0)].x + _prev[new uint3(0, h - 2, 0)].x +
                     _prev[new uint3(0, h - 1, 1)].x) / 3, _prev[id].yz)
                : _prev[id];
            _prev[id] = id.x == 0 && id.y == h - 1 && id.z == d - 1
                ? new float3(
                    (_prev[new uint3(1, h - 1, d - 1)].x + _prev[new uint3(0, h - 2, d - 1)].x +
                     _prev[new uint3(0, h - 1, d - 2)].x) / 3, _prev[id].yz)
                : _prev[id];
            _prev[id] = id.x == w - 1 && id.y == 0 && id.z != 0 && id.z != d - 1
                ? new float3(0.5f * (_prev[new uint3(w - 2, 0, id.z)].x + _prev[new uint3(w - 1, 1, id.z)].x),
                    _prev[id].yz)
                : _prev[id];
            _prev[id] = id.x == w - 1 && id is { y: 0, z: 0 }
                ? new float3(
                    (_prev[new uint3(w - 2, 0, 0)].x + _prev[new uint3(w - 1, 1, 0)].x +
                     _prev[new uint3(w - 1, 0, 1)].x) / 3, _prev[id].yz)
                : _prev[id];
            _prev[id] = id.x == w - 1 && id.y == 0 && id.z == d - 1
                ? new float3(
                    (_prev[new uint3(w - 2, 0, d - 1)].x + _prev[new uint3(w - 1, 1, d - 1)].x +
                     _prev[new uint3(w - 1, 0, d - 2)].x) / 3, _prev[id].yz)
                : _prev[id];
            _prev[id] = id.x == w - 1 && id.y == h - 1 && id.z != 0 && id.z != d - 1
                ? new float3(0.5f * (_prev[new uint3(w - 2, h - 1, id.z)].x + _prev[new uint3(w - 1, h - 2, id.z)].x),
                    _prev[id].yz)
                : _prev[id];
            _prev[id] = id.x == w - 1 && id.y == h - 1 && id.z == 0
                ? new float3(
                    (_prev[new uint3(w - 2, h - 1, 0)].x + _prev[new uint3(w - 1, h - 2, 0)].x +
                     _prev[new uint3(w - 1, h - 1, 1)].x) / 3, _prev[id].yz)
                : _prev[id];
            _prev[id] = id.x == w - 1 && id.y == h - 1 && id.z != d - 1
                ? new float3(
                    (_prev[new uint3(w - 2, h - 1, d - 1)].x + _prev[new uint3(w - 1, h - 2, d - 1)].x +
                     _prev[new uint3(w - 1, h - 1, d - 2)].x) / 3, _prev[id].yz)
                : _prev[id];
        }

        #endregion
    }
}