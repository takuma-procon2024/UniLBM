using System;
using System.Diagnostics;
using Effector.Impl;
using Solver.Impls;
using Unity.Mathematics;
using UnityEngine;

namespace Solver
{
    public class LbmSolverBehaviour : MonoBehaviour
    {
        [Header("コンピュートシェーダー")] [SerializeField]
        private ComputeShader lbmShader, effectorShader;

        [Header("エフェクターのマテリアル")] [SerializeField]
        private Material effectorMaterial;

        [Header("シミュレーションのセルサイズ")] [SerializeField]
        private uint cellSize = 50;

        [Header("粘性係数: 大きいほど粘性が高い")] [SerializeField]
        private float tau = 0.91f;

        [Header("初期速度")] [SerializeField] private Vector3 force = new(0.0002f, 0, 0);

        [Header("使用するソルバー (D3Q15推奨)")] [SerializeField]
        private Solvers solverType;

        [Header("最大パーティクル数")] [SerializeField] private uint maxPoints = 100000;

        [Header("パーティクルの移動速度")] [SerializeField]
        private float moveSpeed = 500f;

        [Header("色相の変化倍率")] [SerializeField] private float hueSpeed = 100f;

        [Header("S: 彩度, V: 明度, A: Alpha値")] [SerializeField]
        private float3 sva = new(1f, 1f, 1f);

        private PointEffector _effector;
        public UniLbmSolverBase Solver { get; private set; }

        private void Start()
        {
            Solver = solverType switch
            {
                Solvers.D3Q15 => new ComputeD3Q15Solver(lbmShader, cellSize, tau, force),
                Solvers.D3Q19 => new ComputeD3Q19Solver(lbmShader, cellSize, tau),
                _ => throw new NotImplementedException()
            };
            _effector = new PointEffector(cellSize, maxPoints, effectorShader,
                effectorMaterial, Solver.GetFieldBuffer(), Solver.GetVelocityBuffer());

            _effector.MoveSpeed = moveSpeed;
            _effector.SetHsvParam(hueSpeed, sva.x, sva.y, sva.z);
        }

        private void Update()
        {
#if UNITY_EDITOR
            CheckParamUpdate();
#endif

            Solver.Step();
            _effector.Update();
        }

        private void OnDestroy()
        {
            Solver?.Dispose();
            _effector?.Dispose();

            Solver = null;
            _effector = null;
        }

        private enum Solvers
        {
            D3Q15,
            D3Q19
        }
#if UNITY_EDITOR
        private float3 _prevSva;
        private float _prevHueSpeed;
        private float _prevTau;
        private float _prevMoveSpeed;

        [Conditional("UNITY_EDITOR")]
        private void CheckParamUpdate()
        {
            if (!(_prevSva.Equals(sva) && _prevHueSpeed.Equals(hueSpeed)))
            {
                _prevSva = sva;
                _prevHueSpeed = hueSpeed;
                _prevTau = tau;

                _effector.SetHsvParam(hueSpeed, sva.x, sva.y, sva.z);
            }
            else if (!_prevTau.Equals(tau))
            {
                _prevTau = tau;
                // _solver.SetTau(tau);
            }
            else if (!_prevMoveSpeed.Equals(moveSpeed))
            {
                _prevMoveSpeed = moveSpeed;
                _effector.MoveSpeed = moveSpeed;
            }
        }
#endif
    }
}