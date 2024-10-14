using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI.DisplayConfig
{
    public class CameraField : MonoBehaviour
    {
        [SerializeField] private TMP_Text cameraNameLabel;
        [SerializeField] private TMP_Dropdown displayDropdown;

        public string CameraName
        {
            get => cameraNameLabel.text;
            set => cameraNameLabel.text = value;
        }

        public int DisplayIndex
        {
            get => displayDropdown.value;
            set => displayDropdown.value = value;
        }

        private void Start()
        {
            displayDropdown.onValueChanged.AddListener(OnChangeDisplay);
        }

        private void OnChangeDisplay(int index)
        {
            var success = OnDisplayChanged?.Invoke(index);
            if (!success.HasValue || !success.Value)
                DisplayIndex = DisplayIndex;
        }

        public void SetDisplayOptions(List<string> options)
        {
            displayDropdown.ClearOptions();
            displayDropdown.AddOptions(options);
        }

        public event Func<int, bool> OnDisplayChanged;
    }
}