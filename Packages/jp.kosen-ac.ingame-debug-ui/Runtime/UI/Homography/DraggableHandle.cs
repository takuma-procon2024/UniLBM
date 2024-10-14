using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Homography
{
    /// <summary>
    ///     一定の範囲内をドラッグ可能なハンドルUI
    /// </summary>
    public class DraggableHandle : MonoBehaviour, IDragHandler
    {
        public enum Pos
        {
            P00,
            P01,
            P10,
            P11
        }

        [SerializeField] private float2 padding;
        [SerializeField] private Pos defaultPos;
        private bool _isInitialized;

        private float2 _min, _max;
        private RectTransform _rectTransform;
        public Pos PosType => defaultPos;

        public float2 NormalizedPos
        {
            get => new(
                (_rectTransform.anchoredPosition.x - _min.x) / (_max.x - _min.x),
                (_rectTransform.anchoredPosition.y - _min.y) / (_max.y - _min.y)
            );
            set
            {
                Initialize();
                ClampAndSetPos(new float2(
                    math.lerp(_min.x, _max.x, value.x),
                    math.lerp(_min.y, _max.y, value.y)
                ));
            }
        }

        private void Start()
        {
            Initialize();
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if (_rectTransform == null) return;
            Handles.Label(transform.position, NormalizedPos.ToString());
        }
#endif

        public void OnDrag(PointerEventData eventData)
        {
            var pos = _rectTransform.anchoredPosition;
            ClampAndSetPos(pos + eventData.delta);
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            TryGetComponent(out _rectTransform);
            ClampAndSetPos(_rectTransform.anchoredPosition);

            var uiRoot = GetComponentInParent<HomographyImageUI>();
            uiRoot.TryGetComponent(out RectTransform rootRect);
            _min = -(float2)rootRect.sizeDelta / 2;
            _max = -_min;
        }

        public void MoveDefaultPos()
        {
            Initialize();
            ClampAndSetPos(defaultPos switch
            {
                Pos.P00 => _min,
                Pos.P01 => new float2(_min.x, _max.y),
                Pos.P10 => new float2(_max.x, _min.y),
                Pos.P11 => _max,
                _ => _rectTransform.anchoredPosition
            });
        }

        private void ClampAndSetPos(in float2 pos)
        {
            Initialize();
            _rectTransform.anchoredPosition = math.clamp(pos, _min - padding, _max + padding);
        }
    }
}