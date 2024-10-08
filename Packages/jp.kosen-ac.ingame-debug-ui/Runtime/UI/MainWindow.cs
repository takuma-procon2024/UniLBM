using LitMotion;
using Unity.Mathematics;
using UnityEngine;

namespace UI
{
    public class MainWindow : MonoBehaviour
    {
        [SerializeField] private string dataFilePath = "data.json";
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private float2 defaultPos = 0;
        [SerializeField] private float2 activePos = 0;
        private bool _isOpen;
        private MotionHandle _motionHandle;
        private RectTransform _rectTransform;

        public DataStore.DataStore DataStore { get; private set; }

        private void Start()
        {
            var path = Application.dataPath + "/" + dataFilePath;
            DataStore = new DataStore.DataStore(path);
            TryGetComponent(out _rectTransform);

            _rectTransform.anchoredPosition = defaultPos;
        }

        private void Update()
        {
            OpenAndClose();
        }

        private void OnDestroy()
        {
            DataStore.Dispose();
        }

        private void OpenAndClose()
        {
            if (_motionHandle.IsActive()) return;
            if (!IsPressOpenKey()) return;

            _motionHandle =
                LMotion.Create(_isOpen ? activePos : defaultPos, _isOpen ? defaultPos : activePos, 0.3f)
                    .WithEase(Ease.InOutQuad)
                    .Bind(v => _rectTransform.anchoredPosition = v);
            _isOpen = !_isOpen;
        }

        private bool IsPressOpenKey()
        {
            return Input.GetKeyDown(toggleKey);
        }
    }
}