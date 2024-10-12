using System;
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
        [SerializeField] private float2 min, max;

        private RectTransform _rectTransform;

        public float2 NormalizedPos => new(
            (_rectTransform.anchoredPosition.x - min.x) / (max.x - min.x),
            (_rectTransform.anchoredPosition.y - min.y) / (max.y - min.y)
        );

        private void OnDrawGizmos()
        {
            if (_rectTransform == null) return;
            Handles.Label(transform.position, $"NormalizedPos: {NormalizedPos}");
        }

        private void Start()
        {
            TryGetComponent(out _rectTransform);
            SetPos(_rectTransform.anchoredPosition);
        }

        public void OnDrag(PointerEventData eventData)
        {
            var pos = _rectTransform.anchoredPosition;
            SetPos(pos + eventData.delta);
        }

        private void SetPos(in float2 pos)
        {
            _rectTransform.anchoredPosition = new float2(
                math.clamp(pos.x, min.x, max.x),
                math.clamp(pos.y, min.y, max.y)
            );
        }
    }
}