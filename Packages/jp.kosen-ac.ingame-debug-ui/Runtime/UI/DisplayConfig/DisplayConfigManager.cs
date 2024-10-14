using System.Collections.Generic;
using UnityEngine;

namespace UI.DisplayConfig
{
    public class DisplayConfigManager : MonoBehaviour
    {
        [SerializeField] private Camera[] targetCameras;
        private Dictionary<string, Camera> _camTable;

        public string[] CamNames { get; private set; }

        private void Start()
        {
            CamNames = new string[targetCameras.Length];
            for (var i = 0; i < targetCameras.Length; i++)
            {
                CamNames[i] = targetCameras[i].name;
                _camTable.Add(CamNames[i], targetCameras[i]);
            }
        }

        public void SetCameraActive(string camName, bool active)
        {
            if (_camTable.TryGetValue(camName, out var value))
                value.enabled = active;
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