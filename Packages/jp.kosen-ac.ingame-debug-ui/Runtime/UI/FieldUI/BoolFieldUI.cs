using System.Collections;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.FieldUI
{
    public class BoolFieldUI : MonoBehaviour
    {
        [SerializeField] private RectTransform handle;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text valueText;

        private UIClickHandler _handler;
        private bool _value;

        public string Label
        {
            get => label.text;
            set => label.text = value;
        }

        public bool Value
        {
            get => _value;
            set
            {
                _value = value;
                valueText.text = value ? "ON" : "OFF";
                StartCoroutine(MoveHandleCoroutine(Value ? 0f : 1f, Value ? 1f : 0f, 0.1f));
            }
        }

        private void Start()
        {
            _handler = background.gameObject.AddComponent<UIClickHandler>();
            _handler.OnClick += OnClick;
        }

        private void OnDestroy()
        {
            _handler.OnClick -= OnClick;
        }

        private void OnClick(PointerEventData data)
        {
            Value = !Value;
        }

        #region Easing

        private static float EaseInOutQuad(float x)
        {
            return x < 0.5f ? 2.0f * x * x : 1.0f - math.pow(-2.0f * x + 2.0f, 2.0f) / 2.0f;
        }

        private IEnumerator MoveHandleCoroutine(float start, float end, float duration)
        {
            var time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                var t = time / duration;
                var et = EaseInOutQuad(t);
                handle.anchorMin = new Vector2(math.lerp(start, end, et), 0.5f);
                handle.anchorMax = new Vector2(math.lerp(start, end, et), 0.5f);
                handle.pivot = new Vector2(math.lerp(start, end, et), 0.5f);
                yield return null;
            }
        }

        #endregion
    }
}