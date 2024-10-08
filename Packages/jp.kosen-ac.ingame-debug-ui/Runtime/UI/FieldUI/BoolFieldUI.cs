using LitMotion;
using TMPro;
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
                LMotion.Create(value ? 0f : 1f, value ? 1f : 0f, 0.1f)
                    .WithEase(Ease.InOutQuad)
                    .Bind(v =>
                    {
                        handle.anchorMin = new Vector2(v, 0.5f);
                        handle.anchorMax = new Vector2(v, 0.5f);
                        handle.pivot = new Vector2(v, 0.5f);
                    });
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
    }
}