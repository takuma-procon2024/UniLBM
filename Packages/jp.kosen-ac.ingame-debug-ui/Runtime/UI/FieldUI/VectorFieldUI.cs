using System;
using System.Globalization;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace UI.FieldUI
{
    public class VectorFieldUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField[] inputFields = new TMP_InputField[4];
        [SerializeField] private TMP_Text label;

        private float4 _value;

        public string Label
        {
            get => label.text;
            set => label.text = value;
        }

        public float4 Value
        {
            get
            {
                try
                {
                    _value = new float4(
                        float.Parse(inputFields[0].text, CultureInfo.InvariantCulture),
                        float.Parse(inputFields[1].text, CultureInfo.InvariantCulture),
                        float.Parse(inputFields[2].text, CultureInfo.InvariantCulture),
                        float.Parse(inputFields[3].text, CultureInfo.InvariantCulture)
                    );
                    return _value;
                }
                catch (Exception)
                {
                    return _value;
                }
            }
            set
            {
                inputFields[0].text = value.x.ToString(CultureInfo.InvariantCulture);
                inputFields[1].text = value.y.ToString(CultureInfo.InvariantCulture);
                inputFields[2].text = value.z.ToString(CultureInfo.InvariantCulture);
                inputFields[3].text = value.w.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void Start()
        {
            foreach (var field in inputFields)
                field.contentType = TMP_InputField.ContentType.DecimalNumber;
        }
    }
}