using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FanDebug
{
    public class PowerSliderUI : MonoBehaviour
    {
        [SerializeField] private int2 range = new(0, 1);
        [SerializeField] private TMP_Text topLabel, bottomLabel, valueLabel;
        [SerializeField] private float2 saturateAndValue = new(0.5f, 1);

        private Slider _slider;
        public int Value { get; private set; }
        public float NormalValue => _slider?.normalizedValue ?? 0;
        public Color Color => Color.HSVToRGB(NormalValue * 0.8f, saturateAndValue.x, saturateAndValue.y);

        private void Start()
        {
            _slider = GetComponentInChildren<Slider>();
            _slider.minValue = range.x;
            _slider.maxValue = range.y;
            _slider.onValueChanged.AddListener(OnSliderChange);

            topLabel.text = range.y.ToString();
            bottomLabel.text = range.x.ToString();

            OnSliderChange(0);
        }

        private void OnSliderChange(float value)
        {
            Value = (int)value;
            valueLabel.text = ((int)value).ToString();

            var colors = _slider.colors;
            colors.selectedColor = colors.normalColor = Color;

            var pressed = colors.selectedColor * 0.8f;
            pressed.a = 1;
            colors.pressedColor = pressed;
            _slider.colors = colors;
        }
    }
}