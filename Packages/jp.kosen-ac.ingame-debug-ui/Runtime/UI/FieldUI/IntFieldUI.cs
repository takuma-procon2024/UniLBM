using System.Globalization;
using TMPro;
using UnityEngine;

namespace UI.FieldUI
{
    public class IntFieldUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Text label;

        private int _value;

        public string Label
        {
            get => label.text;
            set => label.text = value;
        }

        public int Value
        {
            get
            {
                if (int.TryParse(inputField.text, out var value))
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

        private void Start()
        {
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        }
    }
}