using System;
using UI.FieldUI;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ClothCorrectCameraUI : MonoBehaviour
    {
        [SerializeField] private Button closeBtn;
        [SerializeField] private FloatFieldUI camXField, camYField;
        [SerializeField] private SliderFieldUI fovField;
        [SerializeField] private InGameDebugWindow inGameDebugWindow;

        private DataStore.DataStore _dataStore;

        private void Start()
        {
            _dataStore = inGameDebugWindow.DataStore;
            LoadData();

            closeBtn.onClick.AddListener(CloseWindow);
        }

        private void Update()
        {
            SaveData();
        }

        private void CloseWindow()
        {
            gameObject.SetActive(false);
        }

        private void LoadData()
        {
            fovField.Range = new float2(5, 179);
            
            if (_dataStore.TryGetData("CamX", out float camX)) camXField.Value = camX;
            if (_dataStore.TryGetData("CamY", out float camY)) camYField.Value = camY;
            if (_dataStore.TryGetData("Fov", out float fov)) fovField.Value = fov;
        }

        private void SaveData()
        {
            _dataStore.SetData("CamX", camXField.Value);
            _dataStore.SetData("CamY", camYField.Value);
            _dataStore.SetData("Fov", fovField.Value);
        }

        #region Properties

        public float CamX
        {
            get => camXField.Value;
            set => camXField.Value = value;
        }

        public float CamY
        {
            get => camYField.Value;
            set => camYField.Value = value;
        }

        public float Fov
        {
            get => fovField.Value;
            set => fovField.Value = value;
        }

        #endregion
    }
}