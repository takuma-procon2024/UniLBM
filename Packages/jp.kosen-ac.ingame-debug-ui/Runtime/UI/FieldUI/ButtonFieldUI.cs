using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FieldUI
{
    public class ButtonFieldUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button button;

        public string Label
        {
            get => label.text;
            set => label.text = value;
        }

        private void Start()
        {
            button.onClick.AddListener(() => OnClick?.Invoke());
        }

        private void OnDestroy()
        {
            button.onClick.RemoveAllListeners();
        }

        public event Action OnClick;
    }
}