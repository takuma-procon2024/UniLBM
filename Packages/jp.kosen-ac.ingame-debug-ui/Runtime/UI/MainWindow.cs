using System.Collections.Generic;
using LitMotion;
using UI.FieldUI;
using Unity.Mathematics;
using UnityEngine;

namespace UI
{
    public class MainWindow : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private string dataFilePath = "data.json";

        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private float2 defaultPos = 0;
        [SerializeField] private float2 activePos = 0;
        [SerializeField] private RectTransform content;

        [Space] [Header("Resources")] [SerializeField]
        private FloatFieldUI floatFieldPrefab;

        [SerializeField] private IntFieldUI intFieldPrefab;
        [SerializeField] private BoolFieldUI boolFieldPrefab;
        [SerializeField] private StringFieldUI stringFieldPrefab;
        [SerializeField] private SliderFieldUI sliderFieldPrefab;
        private DataStore.DataStore _dataStore;

        private bool _isOpen;
        private MotionHandle _motionHandle;
        private RectTransform _rectTransform;

        private void Start()
        {
            var path = Application.dataPath + "/" + dataFilePath;
            _dataStore = new DataStore.DataStore(path);
            TryGetComponent(out _rectTransform);

            _rectTransform.anchoredPosition = defaultPos;

            AddField("Float Field", 0.5f);
        }

        private void Update()
        {
            OpenAndClose();
        }

        private void OnDestroy()
        {
            ApplyValueToDataStore();
            _dataStore.Dispose();
        }

        private void OpenAndClose()
        {
            if (_motionHandle.IsActive()) return;
            if (!IsPressOpenKey()) return;

            _motionHandle =
                LMotion.Create(_isOpen ? activePos : defaultPos, _isOpen ? defaultPos : activePos, 0.3f)
                    .WithEase(Ease.InOutQuad)
                    .Bind(v => _rectTransform.anchoredPosition = v);
            _isOpen = !_isOpen;
        }

        private bool IsPressOpenKey()
        {
            return Input.GetKeyDown(toggleKey);
        }

        private void ApplyValueToDataStore()
        {
            foreach (var field in _floatFields) _dataStore.SetData(field.Key, field.Value.Value);
            foreach (var field in _intFields) _dataStore.SetData(field.Key, field.Value.Value);
            foreach (var field in _boolFields) _dataStore.SetData(field.Key, field.Value.Value);
            foreach (var field in _stringFields) _dataStore.SetData(field.Key, field.Value.Value);
            foreach (var field in _sliderFields) _dataStore.SetData(field.Key, field.Value);
        }

        #region Get Field & Set Field

        public bool TryGetField(string fieldName, out float value)
        {
            if (_floatFields.TryGetValue(fieldName, out var field))
            {
                value = field.Value;
                return true;
            }

            if (_sliderFields.TryGetValue(fieldName, out var sliderField))
            {
                value = sliderField.Value;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGetField(string fieldName, out int value)
        {
            if (_intFields.TryGetValue(fieldName, out var field))
            {
                value = field.Value;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGetField(string fieldName, out bool value)
        {
            if (_boolFields.TryGetValue(fieldName, out var field))
            {
                value = field.Value;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGetField(string fieldName, out string value)
        {
            if (_stringFields.TryGetValue(fieldName, out var field))
            {
                value = field.Value;
                return true;
            }

            value = default;
            return false;
        }

        public bool TrySetField(string fieldName, float value)
        {
            if (_floatFields.TryGetValue(fieldName, out var field))
            {
                field.Value = value;
                return true;
            }

            if (_sliderFields.TryGetValue(fieldName, out var sliderField))
            {
                sliderField.Value = value;
                return true;
            }

            return false;
        }

        public bool TrySetField(string fieldName, int value)
        {
            if (_intFields.TryGetValue(fieldName, out var field))
            {
                field.Value = value;
                return true;
            }

            return false;
        }

        public bool TrySetField(string fieldName, bool value)
        {
            if (_boolFields.TryGetValue(fieldName, out var field))
            {
                field.Value = value;
                return true;
            }

            return false;
        }

        public bool TrySetField(string fieldName, string value)
        {
            if (_stringFields.TryGetValue(fieldName, out var field))
            {
                field.Value = value;
                return true;
            }

            return false;
        }

        #endregion

        #region UI Builder Methods

        private readonly Dictionary<string, FloatFieldUI> _floatFields = new();
        private readonly Dictionary<string, IntFieldUI> _intFields = new();
        private readonly Dictionary<string, BoolFieldUI> _boolFields = new();
        private readonly Dictionary<string, StringFieldUI> _stringFields = new();
        private readonly Dictionary<string, SliderFieldUI> _sliderFields = new();


        public void AddField(string fieldName, float value)
        {
            var field = Instantiate(floatFieldPrefab, content);
            field.Label = fieldName;
            field.Value = _dataStore.TryGetData(fieldName, out float data) ? data : value;

            _floatFields.Add(fieldName, field);
        }

        public void AddField(string fieldName, int value)
        {
            var field = Instantiate(intFieldPrefab, content);
            field.Label = fieldName;
            field.Value = _dataStore.TryGetData(fieldName, out int data) ? data : value;

            _intFields.Add(fieldName, field);
        }

        public void AddField(string fieldName, bool value)
        {
            var field = Instantiate(boolFieldPrefab, content);
            field.Label = fieldName;
            field.Value = _dataStore.TryGetData(fieldName, out bool data) ? data : value;

            _boolFields.Add(fieldName, field);
        }

        public void AddField(string fieldName, string value)
        {
            var field = Instantiate(stringFieldPrefab, content);
            field.Label = fieldName;
            field.Value = _dataStore.TryGetData(fieldName, out string data) ? data : value;

            _stringFields.Add(fieldName, field);
        }

        public void AddField(string fieldName, float value, float min, float max)
        {
            var field = Instantiate(sliderFieldPrefab, content);
            field.Label = fieldName;
            field.Value = _dataStore.TryGetData(fieldName, out float data) ? data : value;
            field.Range = new float2(min, max);

            _sliderFields.Add(fieldName, field);
        }

        #endregion
    }
}