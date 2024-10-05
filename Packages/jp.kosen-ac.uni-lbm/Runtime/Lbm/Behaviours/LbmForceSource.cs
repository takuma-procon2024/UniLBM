using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Lbm.Behaviours
{
    /// <summary>
    /// LBMの力源を表すコンポーネント
    /// </summary>
    public class LbmForceSource : MonoBehaviour
    {
        [SerializeField] private float forcePower = 1.0f;
        [SerializeField] private uint3 cellSize = 1;

        public float3 Force => transform.forward * forcePower;
        public uint3 CellSize => cellSize;
    }
}