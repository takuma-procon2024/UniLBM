using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class UIClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke(eventData);
        }

        public event Action<PointerEventData> OnClick;
    }
}