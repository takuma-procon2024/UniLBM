using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(Button))]
    public class DebugWindowOpenButton : MonoBehaviour
    {
        private InGameDebugWindow _debugWindow;
        private Image _image;

        private void Start()
        {
            TryGetComponent(out Button button);
            TryGetComponent(out _image);

            _debugWindow = FindAnyObjectByType<InGameDebugWindow>();
            button.onClick.AddListener(() =>
            {
                if (!IsButtonEnable()) return;
                _debugWindow.Open();
            });
        }

        private void Update()
        {
            _image.enabled = IsButtonEnable();
        }

        private bool IsButtonEnable()
        {
            return !_debugWindow.IsOpen && !_debugWindow.IsOtherDebugWindowOpen;
        }
    }
}