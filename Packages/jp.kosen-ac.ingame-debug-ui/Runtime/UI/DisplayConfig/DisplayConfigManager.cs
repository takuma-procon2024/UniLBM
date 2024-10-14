using System.Collections.Generic;
using UnityEngine;

namespace UI.DisplayConfig
{
    public class DisplayConfigManager : MonoBehaviour
    {
        private readonly Dictionary<string, Camera> _camTable = new();
        private readonly Dictionary<string, Canvas> _canvasTable = new();

        public IReadOnlyCollection<Camera> Cameras => _camTable.Values;
        public IReadOnlyCollection<Canvas> Canvases => _canvasTable.Values;

        private void Start()
        {
            var targetCameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var cam in targetCameras) _camTable.Add(cam.name, cam);

            var targetCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var canvas in targetCanvases) _canvasTable.Add(canvas.name, canvas);

            var ui = FindAnyObjectByType<DisplayConfigUI>(FindObjectsInactive.Include);
            var debugWindow = FindAnyObjectByType<InGameDebugWindow>();

            // InGameDebugWindowにボタンを追加
            if (debugWindow != null)
                debugWindow.AddField("Display Config", () =>
                {
                    debugWindow.Close();
                    ui.gameObject.SetActive(true);
                });
            else Debug.LogWarning("InGameDebugWindow not found");

            // DisplayConfigUIを初期化
            if (ui != null) ui.Initialize(this, debugWindow);
            else Debug.LogWarning("DisplayConfigUI not found");
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
        
        public bool TrySetCanvasTargetDisplay(string canvasName, int display)
        {
            var displayCount = Display.displays.Length;
            if (display < 0 || display >= displayCount)
                return false;
            if (_canvasTable.TryGetValue(canvasName, out var value))
                value.targetDisplay = display;
            return true;
        }
    }
}