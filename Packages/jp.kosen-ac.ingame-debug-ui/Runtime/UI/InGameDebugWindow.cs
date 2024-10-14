using System;
using System.Collections;
using System.Collections.Generic;
using UI.FieldUI;
using Unity.Mathematics;
using UnityEngine;

namespace UI
{
    public class InGameDebugWindow : MonoBehaviour
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
        [SerializeField] private ButtonFieldUI buttonFieldPrefab;
        public bool IsOpen { get; private set; }
        public bool IsOtherDebugWindowOpen { get; set; }
        private RectTransform _rectTransform;

        public DataStore.DataStore DataStore { get; private set; }

        private void Awake()
        {
            var path = Application.dataPath + "/" + dataFilePath;
            DataStore = new DataStore.DataStore(path);
            TryGetComponent(out _rectTransform);

            _rectTransform.anchoredPosition = defaultPos;
        }

        private void Update()
        {
            OpenAndClose();
        }

        private void OnDestroy()
        {
            ApplyValueToDataStore();
            DataStore.Dispose();
        }

        private void OpenAndClose()
        {
            if (IsInMotion()) return;
            if (!IsPressOpenKey()) return;

            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (IsOpen || IsOtherDebugWindowOpen) return;
            MoveWindow(defaultPos, activePos, 0.3f);
            IsOpen = true;
        }

        public void Close()
        {
            if (!IsOpen) return;
            MoveWindow(activePos, defaultPos, 0.3f);
            IsOpen = false;
        }

        private bool IsPressOpenKey()
        {
            return Input.GetKeyDown(toggleKey);
        }

        private void ApplyValueToDataStore()
        {
            foreach (var field in _floatFields) DataStore.SetData(field.Key, field.Value.Value);
            foreach (var field in _intFields) DataStore.SetData(field.Key, field.Value.Value);
            foreach (var field in _boolFields) DataStore.SetData(field.Key, field.Value.Value);
            foreach (var field in _stringFields) DataStore.SetData(field.Key, field.Value.Value);
            foreach (var field in _sliderFields) DataStore.SetData(field.Key, field.Value);
        }

        #region Util

        private IEnumerator _moveWindowHandle;

        private static float EaseInOutQuad(float x)
        {
            return x < 0.5f ? 2.0f * x * x : 1.0f - math.pow(-2.0f * x + 2.0f, 2.0f) / 2.0f;
        }

        private bool IsInMotion()
        {
            return _moveWindowHandle != null;
        }

        private void MoveWindow(in float2 start, in float2 end, float duration)
        {
            if (IsInMotion()) return;
            _moveWindowHandle = MoveWindowCoroutine(start, end, duration);
            StartCoroutine(_moveWindowHandle);
        }

        private IEnumerator MoveWindowCoroutine(float2 start, float2 end, float duration)
        {
            var time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                var t = math.saturate(time / duration);
                _rectTransform.anchoredPosition = math.lerp(start, end, EaseInOutQuad(t));
                yield return null;
            }

            _moveWindowHandle = null;
        }

        #endregion

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

        public void AddField(string fieldName, in Action onClick)
        {
            var field = Instantiate(buttonFieldPrefab, content);
            field.transform.SetAsFirstSibling();
            field.Label = fieldName;
            field.OnClick += onClick;
        }

        public void AddField(string fieldName, float value)
        {
            var field = Instantiate(floatFieldPrefab, content);
            field.Label = fieldName;
            field.Value = DataStore.TryGetData(fieldName, out float data) ? data : value;

            _floatFields.Add(fieldName, field);
        }

        public void AddField(string fieldName, int value)
        {
            var field = Instantiate(intFieldPrefab, content);
            field.Label = fieldName;
            field.Value = DataStore.TryGetData(fieldName, out int data) ? data : value;

            _intFields.Add(fieldName, field);
        }

        public void AddField(string fieldName, bool value)
        {
            var field = Instantiate(boolFieldPrefab, content);
            field.Label = fieldName;
            field.Value = DataStore.TryGetData(fieldName, out bool data) ? data : value;

            _boolFields.Add(fieldName, field);
        }

        public void AddField(string fieldName, string value)
        {
            var field = Instantiate(stringFieldPrefab, content);
            field.Label = fieldName;
            field.Value = DataStore.TryGetData(fieldName, out string data) ? data : value;

            _stringFields.Add(fieldName, field);
        }

        public void AddField(string fieldName, float value, float min, float max)
        {
            var field = Instantiate(sliderFieldPrefab, content);
            field.Label = fieldName;
            field.Value = DataStore.TryGetData(fieldName, out float data) ? data : value;
            field.Range = new float2(min, max);

            _sliderFields.Add(fieldName, field);
        }

        #endregion
    }
}