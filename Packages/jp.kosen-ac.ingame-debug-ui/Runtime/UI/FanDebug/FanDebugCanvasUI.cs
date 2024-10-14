using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.FanDebug
{
    public class FanDebugCanvasUI : MonoBehaviour, IDragHandler
    {
        [SerializeField] private GridLayoutGroup fanRoot, subFanRoot;
        [SerializeField] private float fanRadius = 0.1f;
        [SerializeField] private PowerSliderUI powerSlider;
        [SerializeField] private int fanCount = 45;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject fanDebugUI;

        private readonly Vector3[] _corners = new Vector3[4];
        private Image[] _fans;
        private InGameDebugWindow _inGameDebugWindow;
        private RenderTexture _prevCanvas, _currCanvas;
        private RectTransform _rectTransform;
        private IFanSetter _setter;

        private void Start()
        {
            _inGameDebugWindow = FindAnyObjectByType<InGameDebugWindow>();
            TryGetComponent(out _setter);
            TryGetComponent(out _rectTransform);
            _rectTransform.GetWorldCorners(_corners);

            _fans = new Image[fanCount];
            var cnt = 0;
            for (var i = 0; i < fanRoot.transform.childCount; i++)
                if (fanRoot.transform.GetChild(i).TryGetComponent(out _fans[cnt]))
                    cnt++;
            for (var i = 0; i < subFanRoot.transform.childCount; i++)
                if (subFanRoot.transform.GetChild(i).TryGetComponent(out _fans[cnt]))
                    cnt++;
            Assert.IsTrue(fanCount == cnt);

            foreach (var fan in _fans) fan.color = Color.blue;

            closeButton.onClick.AddListener(() => fanDebugUI.SetActive(false));
        }

        private void OnEnable()
        {
            _inGameDebugWindow.IsOtherDebugWindowOpen = true;
        }

        private void OnDisable()
        {
            _inGameDebugWindow.IsOtherDebugWindowOpen = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            var normalPos = NormalizePosition(eventData.position);

            var index = 0;
            foreach (var fan in _fans)
            {
                var fanPos = NormalizePosition(((float3)fan.transform.position).xy);
                var distanceSq = math.distancesq(normalPos, fanPos);
                if (distanceSq < fanRadius * fanRadius)
                {
                    fan.color = powerSlider.Color;
                    _setter.SetFanPower(powerSlider.NormalValue, index);
                }

                index++;
            }
        }

        private float2 NormalizePosition(in float2 position)
        {
            return new float2(
                math.unlerp(_corners[0].x, _corners[2].x, position.x),
                math.unlerp(_corners[0].y, _corners[2].y, position.y)
            );
        }
    }
}