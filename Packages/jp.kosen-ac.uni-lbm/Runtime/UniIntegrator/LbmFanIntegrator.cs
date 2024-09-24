using System;
using Lbm;
using Unity.Mathematics;
using UnityEngine;

namespace UniIntegrator
{
    /// <summary>
    ///     ファンの風の大きさをLBMソルバーに伝えるためのコンポーネント
    /// </summary>
    public class LbmFanIntegrator : MonoBehaviour
    {
        [Header("Properties")] [Header("配置するファンの数")] [SerializeField]
        private uint2 fanNum;

        [Header("ファンが風を出すセルの大きさ")] [SerializeField]
        private uint3 fanSize;

        [Header("ファンの風の大きさ")] [SerializeField] private float3 fanPower;

        [Header("ファン同士の間隔 (割合指定 0 ~ 1)")] [SerializeField]
        private float fanMargin;

        [Header("ファンを配置するオフセット")] [SerializeField]
        private float3 fanOffset;

        [Header("Resources")] [Header("LBMソルバー")] [SerializeField]
        private LbmSolverBehaviour lbmSolver;

        private float[] fanPowers;

        private void Start()
        {
            fanPowers = new float[fanNum.x * fanNum.y];
        }

        /// <summary>
        ///     ファンパワーを設定する
        /// </summary>
        /// <param name="getPower">引数として渡されたIDに基づくファンの強さを返す関数</param>
        public void SetFanPowers(in Func<uint2, float> getPower)
        {
            for (var y = 0; y < fanNum.y; y++)
            for (var x = 0; x < fanNum.x; x++)
                fanPowers[y * fanNum.x + x] = getPower(new uint2((uint)x, (uint)y));
        }
    }
}