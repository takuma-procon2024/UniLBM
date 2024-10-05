using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Lbm.Behaviours
{
    /// <summary>
    /// LBMの力源を表すコンポーネント
    /// </summary>
    public class LbmForceSource : MonoBehaviour
    {
        [SerializeField] private float3 force = float3.zero;
    }
}