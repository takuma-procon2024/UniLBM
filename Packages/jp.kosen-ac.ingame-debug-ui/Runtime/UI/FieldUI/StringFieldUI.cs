using TMPro;
using UnityEngine;

namespace UI.FieldUI
{
    public class StringFieldUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Text label;

        public string Label
        {
            get => label.text;
            set => label.text = value;
        }

        public string Value
        {
            get => inputField.text;
            set => inputField.text = value;
        }
    }
}