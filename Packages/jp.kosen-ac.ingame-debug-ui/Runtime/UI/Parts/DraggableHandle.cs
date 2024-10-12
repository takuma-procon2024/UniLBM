using UI.Homography;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Parts
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
        
        private float2 _min, _max;
        private RectTransform _rectTransform;
        public Pos PosType => defaultPos;

        public float2 NormalizedPos => new(
            (_rectTransform.anchoredPosition.x - _min.x) / (_max.x - _min.x),
            (_rectTransform.anchoredPosition.y - _min.y) / (_max.y - _min.y)
        );

        private void Start()
        {
            TryGetComponent(out _rectTransform);
            SetPos(_rectTransform.anchoredPosition);

            var uiRoot = GetComponentInParent<HomographyImageUI>();
            uiRoot.TryGetComponent(out RectTransform rootRect);
            _min = -(float2)rootRect.sizeDelta / 2 + padding;
            _max = (float2)rootRect.sizeDelta / 2 - padding;
            
            _rectTransform.anchoredPosition = defaultPos switch
            {
                Pos.P00 => _min,
                Pos.P01 => new float2(_min.x, _max.y),
                Pos.P10 => new float2(_max.x, _min.y),
                Pos.P11 => _max,
                _ => _rectTransform.anchoredPosition
            };
        }

        private void OnDrawGizmos()
        {
            if (_rectTransform == null) return;
            Handles.Label(transform.position, NormalizedPos.ToString());
        }

        public void OnDrag(PointerEventData eventData)
        {
            var pos = _rectTransform.anchoredPosition;
            SetPos(pos + eventData.delta);
        }

        private void SetPos(in float2 pos)
        {
            _rectTransform.anchoredPosition = new float2(
                math.clamp(pos.x, _min.x, _max.x),
                math.clamp(pos.y, _min.y, _max.y)
            );
        }
    }
}