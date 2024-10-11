using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace DataStore
{
    [Serializable]
    public class Data
    {
        public List<FloatField> floatFields = new();
        public List<IntField> intFields = new();
        public List<StringField> stringFields = new();
        public List<BoolField> boolFields = new();
        public List<VectorField> vectorFields = new();
    }

    [Serializable]
    public class FloatField
    {
        public string name;
        public float value;
    }

    [Serializable]
    public class IntField
    {
        public string name;
        public int value;
    }

    [Serializable]
    public class StringField
    {
        public string name;
        public string value;
    }

    [Serializable]
    public class BoolField
    {
        public string name;
        public bool value;
    }

    [Serializable]
    public class VectorField
    {
        public string name;
        public float4 value;
    }
}