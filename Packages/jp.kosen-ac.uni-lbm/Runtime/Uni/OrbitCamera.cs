using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Uni
{
    public class OrbitCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float rotateSpeed = 3.0f;
        [SerializeField] private float touchMoveSpeed = 1.0f;
        [SerializeField] private float moveSpeed = 0.1f;
        [SerializeField] private float touchRotateSpeed = 1f;
        [SerializeField] private float scrollSpeed = 10;
        [SerializeField] private float touchScrollSpeed = 0.1f;
        [SerializeField] private float defaultDistance = 10;
        [SerializeField] private float minDistance = 1;
        [SerializeField] private float maxDistance = 20;
        [SerializeField] private float defaultY = 45;
        [SerializeField] private float defaultX = 45;

        private float _distance, _timer, _x, _y;
        private Vector3 _initTargetPos;
        private bool _isFirstFrame = true;

        /// <summary>
        ///     直前の2点間の距離
        /// </summary>
        private float _prevTouchDistance;

        private void Start()
        {
            _y = defaultY;
            _x = defaultX;
            _distance = defaultDistance;
            _initTargetPos = target.position;

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

            if (!Input.GetMouseButton(0) && !Input.GetMouseButton(2))
            {
                _prevTouchDistance = 0;
                return;
            }
            if (_isFirstFrame)
            {
                _isFirstFrame = false;
                return;
            }

            // 移動
            if (Input.GetMouseButton(2))
            {
                var screenDelta = Vector2.zero;
                screenDelta.x -= Input.GetAxis("Mouse X") * moveSpeed;
                screenDelta.y -= Input.GetAxis("Mouse Y") * moveSpeed;

                var camPos = target.position;
                camPos += transform.right * screenDelta.x;
                camPos += transform.up * screenDelta.y;

                target.position = camPos;
            }

            if (Input.touchCount == 2)
            {
                // ピンチイン・アウト
                var touch0 = Input.GetTouch(0);
                var touch1 = Input.GetTouch(1);
                var touchDistance = math.distance(touch0.position, touch1.position);
                var touchDistanceDiff = _prevTouchDistance - touchDistance;

                if (_prevTouchDistance != 0)
                {
                    _distance += touchDistanceDiff * touchScrollSpeed;
                    _distance = math.clamp(_distance, minDistance, maxDistance);
                }

                // カメラを動かす
                var screenPos = Vector2.zero;
                screenPos.x -= Input.touches[0].deltaPosition.x * touchMoveSpeed;
                screenPos.y -= Input.touches[0].deltaPosition.y * touchMoveSpeed;

                var camPos = target.position;
                camPos += transform.right * screenPos.x;
                camPos += transform.up * screenPos.y;
                target.position = camPos;

                _prevTouchDistance = touchDistance;
            }
            else
            {
                _prevTouchDistance = 0;
                
                if (Input.touchCount == 1 || Input.GetMouseButton(0))
                {
                    // 回転
                    _x += Input.touches.Any()
                        ? Input.touches[0].deltaPosition.x * touchRotateSpeed
                        : Input.GetAxis("Mouse X") * rotateSpeed;
                    _y -= Input.touches.Any()
                        ? Input.touches[0].deltaPosition.y * touchRotateSpeed
                        : Input.GetAxis("Mouse Y") * rotateSpeed;
                }
            }

            if (Input.touchCount == 3)
            {
                // 位置を初期化
                _y = defaultY;
                _x = defaultX;
                _distance = defaultDistance;
                target.position = _initTargetPos;

                RotateCamera();
                transform.LookAt(target);
            }
        }

        private void RotateCamera()
        {
            var rotation = Quaternion.Euler(_y, _x, 0);
            var position = rotation * new Vector3(0.0f, 0.0f, -_distance) + target.position;
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}