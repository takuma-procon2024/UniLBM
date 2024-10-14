using System.Collections.Generic;
using UnityEngine;

namespace UI.DisplayConfig
{
    public class DisplayConfigManager : MonoBehaviour
    {
        private readonly Dictionary<string, Camera> _camTable = new();

        public IReadOnlyCollection<Camera> Cameras => _camTable.Values;

        private void Start()
        {
            var targetCameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var cam in targetCameras) _camTable.Add(cam.name, cam);

            // DisplayConfigUIを探して初期化
            var ui = FindAnyObjectByType<DisplayConfigUI>(FindObjectsInactive.Include);
            if (ui != null) ui.Initialize(this);
            else Debug.LogWarning("DisplayConfigUI not found");

            // InGameDebugWindowを探してボタンを追加
            var debugWindow = FindAnyObjectByType<InGameDebugWindow>();
            if (debugWindow != null)
                debugWindow.AddField("Display Config", () =>
                {
                    debugWindow.Close();
                    ui.gameObject.SetActive(true);
                });
            else Debug.LogWarning("InGameDebugWindow not found");
        }

        public bool TrySetCameraTargetDisplay(string camName, int display)
        {
            var displayCount = Display.displays.Length;
            if (display < 0 || display >= displayCount)
                return false;
            if (_camTable.TryGetValue(camName, out var value))
                value.targetDisplay = display;
            return true;
        }
    }
}