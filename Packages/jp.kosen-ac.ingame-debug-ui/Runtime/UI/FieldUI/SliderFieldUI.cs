using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FieldUI
{
    public class SliderFieldUI : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private TMP_Text label;

        public float2 Range
        {
            get => new(slider.minValue, slider.maxValue);
            set
            {
                slider.minValue = value.x;
                slider.maxValue = value.y;
            }
        }

        public string Label
        {
            get => label.text;
            set => label.text = value;
        }

        public float Value
        {
            get => slider.value;
            set
            {
                slider.value = value;
                valueLabel.text = value.ToString("F2");
            }
        }
    }
}