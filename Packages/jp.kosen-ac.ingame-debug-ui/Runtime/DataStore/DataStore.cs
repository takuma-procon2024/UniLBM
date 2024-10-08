using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DataStore
{
    public class DataStore : IDisposable
    {
        private readonly string _dataFilePath;

        public DataStore(string dataFilePath, Data defaultData = default)
        {
            _dataFilePath = dataFilePath;

            if (File.Exists(dataFilePath))
            {
                Load();
            }
            else
            {
                Data = defaultData;
                Save();
            }
        }


        public void Dispose()
        {
            Save();
        }

        private void Load()
        {
            Debug.Log($"Load from {_dataFilePath}");

            using var reader = new StreamReader(_dataFilePath);
            var json = reader.ReadToEnd();
            Data = JsonUtility.FromJson<Data>(json);
        }

        private void Save()
        {
            Debug.Log($"Save to {_dataFilePath}");

            var json = JsonUtility.ToJson(Data, true);
            using var writer = new StreamWriter(_dataFilePath, false);
            writer.Write(json);
        }

        #region GetData Overloads & Data Fields

        private Data Data
        {
            get => new()
            {
                floatFields = _floatFields.Select(v => new FloatField { name = v.Key, value = v.Value }).ToList(),
                intFields = _intFields.Select(v => new IntField { name = v.Key, value = v.Value }).ToList(),
                stringFields = _stringFields.Select(v => new StringField { name = v.Key, value = v.Value }).ToList(),
                boolFields = _boolFields.Select(v => new BoolField { name = v.Key, value = v.Value }).ToList(),
            };
            set
            {
                _floatFields.Clear();
                _intFields.Clear();
                _stringFields.Clear();
                _boolFields.Clear();
                if (value == null) return;
                foreach (var field in value.floatFields) _floatFields[field.name] = field.value;
                foreach (var field in value.intFields) _intFields[field.name] = field.value;
                foreach (var field in value.stringFields) _stringFields[field.name] = field.value;
                foreach (var field in value.boolFields) _boolFields[field.name] = field.value;
            }
        }

        private readonly Dictionary<string, bool> _boolFields = new();
        private readonly Dictionary<string, float> _floatFields = new();
        private readonly Dictionary<string, int> _intFields = new();
        private readonly Dictionary<string, string> _stringFields = new();

        public bool TryGetData(string name, out float data)
        {
            return _floatFields.TryGetValue(name, out data);
        }

        public bool TryGetData(string name, out int data)
        {
            return _intFields.TryGetValue(name, out data);
        }

        public bool TryGetData(string name, out string data)
        {
            return _stringFields.TryGetValue(name, out data);
        }

        public bool TryGetData(string name, out bool data)
        {
            return _boolFields.TryGetValue(name, out data);
        }

        #endregion
    }
}