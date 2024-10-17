using TMPro;
using UnityEngine;

namespace UI.FieldUI
{
    public class SectionFieldUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        
        public string Label
        {
            get => label.text;
            set => label.text = value;
        }
    }
}