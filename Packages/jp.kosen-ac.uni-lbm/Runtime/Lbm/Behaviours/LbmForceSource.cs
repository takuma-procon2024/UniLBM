using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Lbm.Behaviours
{
    /// <summary>
    ///     LBMの力源を表すコンポーネントの簡易実装
    /// </summary>
    public class LbmForceSource : MonoBehaviour, ILbmForceSource
    {
        [SerializeField] private float forcePower = 1.0f;
        [SerializeField] private uint3 cellSize = 1;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 1);
        }

        public float3 Force => transform.forward * forcePower;
        public uint3 CellSize => cellSize;
        public float3 Position => transform.position;
    }
}