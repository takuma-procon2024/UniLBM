using UnityEngine;

namespace UI.FanDebug
{
    public class FanDebugUIInitializer : MonoBehaviour
    {
        [SerializeField] private InGameDebugWindow inGameDebugWindow;
        [SerializeField] private GameObject fanDebugUI;

        private void Start()
        {
            inGameDebugWindow.AddField("Open Fan Debug", () =>
            {
                inGameDebugWindow.Close();
                fanDebugUI.SetActive(true);
            });
        }
    }
}