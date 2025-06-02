using UniLbm.Lbm.Extension;
using Unity.Mathematics;
using UnityEngine;

namespace Projects.Scripts
{
    public class ExampleForceSource : MonoBehaviour, ILbmForceSource
    {
        [SerializeField] private int index;
        [SerializeField] private uint3 cellSize = 1;

        private float _forcePower;

        public float3 Force => transform.forward * _forcePower;
        public uint3 CellSize => cellSize;
        public float3 Position => transform.position;
        public int Index => index;

        public void SetForce(float force)
        {
            _forcePower = force;
        }
    }
}