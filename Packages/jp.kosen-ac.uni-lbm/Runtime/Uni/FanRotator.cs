using UniLbm.Lbm.Extension;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace UniLbm.Uni
{
    public class FanRotator : MonoBehaviour
    {
        [SerializeField] private float rotateSpeed = 1.0f;

        private ILbmForceSource _forceSource;

        private void Start()
        {
            TryGetComponent(out _forceSource);
            Assert.IsNotNull(_forceSource);
        }

        private void Update()
        {
            var forceLen = math.length(_forceSource.Force);
            transform.rotation *= Quaternion.AngleAxis(rotateSpeed * forceLen, Vector3.forward);
        }
    }
}