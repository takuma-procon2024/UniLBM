using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UI.DisplayConfig
{
    public class DisplayConfigUI : MonoBehaviour
    {
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private CameraField cameraFieldPrefab;
        [SerializeField] private Button closeButton;
        private readonly List<CameraField> _cameraFields = new();
        private DataStore.DataStore _dataStore;
        private InGameDebugWindow _debugWindow;
        private bool _hasInvalidDisplayIndex;
        private DisplayConfigManager _manager;

        private void OnEnable()
        {
            _debugWindow.IsOtherDebugWindowOpen = true;
        }

        private void OnDisable()
        {
            _debugWindow.IsOtherDebugWindowOpen = false;
        }

        public void Initialize(DisplayConfigManager manager, InGameDebugWindow debugWindow)
        {
            _debugWindow = debugWindow;
            _manager = manager;
            _dataStore = debugWindow.DataStore;

            var displayOptions = Display.displays.Select((v, i) => $"Display {i}").ToList();
            foreach (var cam in manager.Cameras)
            {
                var ui = Instantiate(cameraFieldPrefab, contentRoot);
                ui.CameraName = cam.name;
                ui.SetDisplayOptions(displayOptions);
                if (cam.targetDisplay < displayOptions.Count)
                {
                    ui.DisplayIndex = cam.targetDisplay;
                }
                else
                {
                    Debug.LogWarning(
                        $"Camera {cam.name} is set to invalid display index {cam.targetDisplay}. If it's in the Editor, there's no problem.");
                    _hasInvalidDisplayIndex = true;
                }

                ui.OnDisplayChanged += index =>
                {
                    if (!manager.TrySetCameraTargetDisplay(cam.name, index)) return false;
                    if (!_hasInvalidDisplayIndex)
                        SaveDisplayConfig();
                    return true;
                };
                _cameraFields.Add(ui);
            }

            foreach (var canvas in manager.Canvases)
            {
                var ui = Instantiate(cameraFieldPrefab, contentRoot);
                ui.CameraName = $"{canvas.name}(Canvas)";
                ui.SetDisplayOptions(displayOptions);
                if (canvas.targetDisplay < displayOptions.Count)
                {
                    ui.DisplayIndex = canvas.targetDisplay;
                }
                else
                {
                    Debug.LogWarning(
                        $"Camera {canvas.name} is set to invalid display index {canvas.targetDisplay}. If it's in the Editor, there's no problem.");
                    _hasInvalidDisplayIndex = true;
                }

                ui.OnDisplayChanged += index =>
                {
                    if (!manager.TrySetCanvasTargetDisplay(canvas.name, index)) return false;
                    if (!_hasInvalidDisplayIndex)
                        SaveDisplayConfig();
                    return true;
                };
                _cameraFields.Add(ui);
            }

            closeButton.onClick.AddListener(() => gameObject.SetActive(false));

            LoadDisplayConfig();
        }

        private void SaveDisplayConfig()
        {
            foreach (var (cam, camField) in _manager.Cameras.Zip(_cameraFields, (cam, field) => (cam, field)))
            {
                var key = GetDisplayIndexDataKey(cam);
                _dataStore.SetData(key, camField.DisplayIndex);
            }
        }

        private void LoadDisplayConfig()
        {
            foreach (var (cam, camField) in _manager.Cameras.Zip(_cameraFields, (cam, field) => (cam, field)))
            {
                var key = GetDisplayIndexDataKey(cam);
                if (!_dataStore.TryGetData(key, out int displayIndex)) continue;
                if (_manager.TrySetCameraTargetDisplay(cam.name, displayIndex))
                    camField.DisplayIndex = displayIndex;
            }
        }

        private static string GetDisplayIndexDataKey(Camera cam)
        {
            return $"CamConfig.{cam.name}.DisplayIdx";
        }
    }
}