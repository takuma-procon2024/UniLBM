using System;
using UnityEngine;

namespace Common
{
    public class DebugFpsViewer : MonoBehaviour
    {
        private void OnGUI()
        {
            var fps = 1.0f / Time.deltaTime;
            GUI.Label(new Rect(10, 10, 100, 20), $"FPS: {fps}");
        }
    }
}