using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Uni
{
    public class OrbitCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float rotateSpeed = 3.0f;
        [SerializeField] private float scrollSpeed = 10;
        [SerializeField] private float defaultDistance = 10;
        [SerializeField] private float minDistance = 1;
        [SerializeField] private float maxDistance = 20;
        [SerializeField] private float defaultY = 45;
        [SerializeField] private float defaultX = 45;

        private float _distance, _timer, _x, _y;
        private bool _isFirstFrame = true;

        private void Start()
        {
            _y = defaultY;
            _x = defaultX;
            _distance = defaultDistance;

            RotateCamera();
            transform.LookAt(target);
        }

        private void Update()
        {
            RotateCamera();

            var scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            _timer += Time.deltaTime;

            if (math.abs(scrollWheel) > 0 && _timer > 0.01f)
            {
                _timer = 0;
                _distance += scrollSpeed * (scrollWheel > 0 ? -1 : 1);
                _distance = math.clamp(_distance, minDistance, maxDistance);
            }

            if (!Input.GetMouseButton(0)) return;
            if (_isFirstFrame)
            {
                _isFirstFrame = false;
                return;
            }

            _x += Input.GetAxis("Mouse X") * rotateSpeed;
            _y -= Input.GetAxis("Mouse Y") * rotateSpeed;
        }

        private void RotateCamera()
        {
            var rotation = Quaternion.Euler(_y, _x, 0);
            var position = rotation * new Vector3(0.0f, 0.0f, -_distance) + target.position;
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}