using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(Button))]
    public class DebugWindowOpenButton : MonoBehaviour
    {
        private void Start()
        {
            TryGetComponent(out Button button);
            var debugWindow = FindAnyObjectByType<InGameDebugWindow>();
            button.onClick.AddListener(() => debugWindow.Open());

            debugWindow.OnOpen += () => button.gameObject.SetActive(false);
            debugWindow.OnClose += () => button.gameObject.SetActive(true);
        }
    }
}