using UnityEngine;

namespace UniLbm.Uni
{
    public class OrbitCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float rotateSpeed = 3.0f;
        [SerializeField] private float distance = 10;
        [SerializeField] private float maxDistance = 20;

        private float _x;
        private float _y;
        private float scrollWheel;
        private float timer;

        private void Start()
        {
            _y = 45;

            RotateCamera();
            transform.LookAt(target);
        }

        private void Update()
        {
            scrollWheel = Input.GetAxis("Mouse ScrollWheel");

            timer += Time.deltaTime;

            if (scrollWheel > 0 && timer > 0.01f)
            {
                timer = 0;
                distance -= 0.5f;

                if (distance <= 1) distance = 1;

                RotateCamera();
            }

            if (scrollWheel < 0 && timer > 0.01f)
            {
                timer = 0;
                distance += 0.5f;

                if (distance >= maxDistance) distance = maxDistance;

                RotateCamera();
            }

            if (Input.GetMouseButton(0))
            {
                _x += Input.GetAxis("Mouse X") * rotateSpeed;
                _y -= Input.GetAxis("Mouse Y") * rotateSpeed;

                RotateCamera();
            }
        }

        private void RotateCamera()
        {
            var rotation = Quaternion.Euler(_y, _x, 0);
            var position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.position;
            transform.rotation = rotation;
            transform.position = position;
        }
    }
}