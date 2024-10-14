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

        public void Initialize(DisplayConfigManager manager)
        {
            var displayOptions = Display.displays.Select((v, i) => $"Display {i}").ToList();
            foreach (var cam in manager.Cameras)
            {
                var ui = Instantiate(cameraFieldPrefab, contentRoot);
                ui.CameraName = cam.name;
                ui.SetDisplayOptions(displayOptions);
                if (cam.targetDisplay < displayOptions.Count)
                    ui.DisplayIndex = cam.targetDisplay;
                else
                    Debug.LogWarning(
                        $"Camera {cam.name} is set to invalid display index {cam.targetDisplay}. If it's in the Editor, there's no problem.");
                ui.OnDisplayChanged += index => manager.TrySetCameraTargetDisplay(cam.name, index);
            }

            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }
}