using UniLbm.Lbm.Extension;
using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Cloth.Extension
{
    public class LbmToFSensor : MonoBehaviour, ILbmToFSensor
    {
        [SerializeField] private float distance;

        public float3 Position => transform.position;
        public float3 Direction => transform.forward;
        public float Distance => distance;
    }
}