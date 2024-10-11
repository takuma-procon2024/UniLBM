using UI.FieldUI;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ClothCorrectCameraUI : MonoBehaviour
    {
        [SerializeField] private Button closeBtn;
        [SerializeField] private VectorFieldUI camPosField;
        [SerializeField] private VectorFieldUI camProjField;
        [SerializeField] private SliderFieldUI fovField;
        [SerializeField] private FloatFieldUI aspectField;
        [SerializeField] private BoolFieldUI useFrustumField;
        [SerializeField] private InGameDebugWindow inGameDebugWindow;

        private DataStore.DataStore _dataStore;

        private void Update()
        {
            SaveData();
        }

        public void Initialize(Transform cam, in float4 proj, float defaultFov, float defaultAspect)
        {
            _dataStore = inGameDebugWindow.DataStore;
            LoadData(cam.position, proj, defaultFov, defaultAspect);

            gameObject.SetActive(false);
            inGameDebugWindow.AddField("OpenCorrectCamera", () =>
            {
                inGameDebugWindow.Close();
                gameObject.SetActive(true);
            });
            closeBtn.onClick.AddListener(CloseWindow);
        }

        private void CloseWindow()
        {
            gameObject.SetActive(false);
        }

        private void LoadData(in float3 pos, in float4 proj, float defaultFov, float defaultAspect)
        {
            fovField.Range = new float2(1, 179);

            camPosField.Value = _dataStore.TryGetData("CamPos", out float4 camX) ? camX : new float4(pos, 0);
            camProjField.Value = _dataStore.TryGetData("CamProj", out float4 camY) ? camY : proj;
            fovField.Value = _dataStore.TryGetData("Fov", out float fov) ? fov : defaultFov;
            aspectField.Value = _dataStore.TryGetData("Aspect", out float aspect) ? aspect : defaultAspect;
            useFrustumField.Value = _dataStore.TryGetData("UseFrustum", out bool useFrustum) && useFrustum;
        }

        private void SaveData()
        {
            _dataStore.SetData("CamPos", camPosField.Value);
            _dataStore.SetData("CamProj", camProjField.Value);
            _dataStore.SetData("Fov", fovField.Value);
            _dataStore.SetData("Aspect", aspectField.Value);
            _dataStore.SetData("UseFrustum", useFrustumField.Value);
        }

        #region Properties

        public float4 CamPos
        {
            get => camPosField.Value;
            set => camPosField.Value = value;
        }

        public float4 CamProj
        {
            get => camProjField.Value;
            set => camProjField.Value = value;
        }

        public float Fov
        {
            get => fovField.Value;
            set => fovField.Value = value;
        }

        public float Aspect
        {
            get => aspectField.Value;
            set => aspectField.Value = value;
        }

        public bool UseFrustum
        {
            get => useFrustumField.Value;
            set => useFrustumField.Value = value;
        }

        #endregion
    }
}