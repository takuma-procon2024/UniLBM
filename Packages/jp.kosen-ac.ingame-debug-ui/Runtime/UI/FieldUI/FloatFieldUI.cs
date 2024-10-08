using System.Globalization;
using TMPro;
using UnityEngine;

namespace UI.FieldUI
{
    public class FloatFieldUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Text label;
        
        private float _value;

        private void Start()
        {
            inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
        }

        public string Label
        {
            get => label.text;
            set => label.text = value;
        }

        public float Value
        {
            get
            {
                if (float.TryParse(inputField.text, out var value))
                {
                    _value = value;
                    return value;
                }

                Value = _value;
                return _value;
            }
            set
            {
                inputField.text = value.ToString(CultureInfo.InvariantCulture);
                _value = value;
            }
        }
    }
}