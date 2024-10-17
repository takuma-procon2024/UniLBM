using UI;
using UnityEngine;

namespace UniLbm.Projector
{
    public class UnlitCloth : MonoBehaviour
    {
        private ClothCorrectCameraUI _clothCorrectCameraUI;
        private bool _isInitialized;

        private void Update()
        {
            if (!_isInitialized) return;

            transform.SetPositionAndRotation(
                _clothCorrectCameraUI.ClothPos.xyz,
                Quaternion.Euler(_clothCorrectCameraUI.ClothRot.xyz)
            );
            transform.localScale = _clothCorrectCameraUI.ClothSize.xyz;
        }

        public void Initialize(ClothCorrectCameraUI ui)
        {
            _clothCorrectCameraUI = ui;
            _isInitialized = true;
        }
    }
}