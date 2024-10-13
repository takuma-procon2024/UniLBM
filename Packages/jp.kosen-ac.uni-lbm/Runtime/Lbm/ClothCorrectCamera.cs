using UI;
using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Lbm
{
    public class ClothCorrectCamera : MonoBehaviour
    {
        [SerializeField] private Camera correctCamera;
        [SerializeField] private ClothCorrectCameraUI clothCorrectCameraUI;

        private float4 _prevProjParam;

        private void Start()
        {
            ActivateDisplay();
            
            clothCorrectCameraUI.gameObject.SetActive(true);
            clothCorrectCameraUI.Initialize(correctCamera.transform, new float4(1, -1, 1, -1),
                correctCamera.fieldOfView, correctCamera.aspect);
        }

        private void Update()
        {
            correctCamera.transform.position = clothCorrectCameraUI.CamPos.xyz;
            SetCameraProjectionMatrix();
        }

        private void ActivateDisplay()
        {
            Debug.Log($"Displays connected: {Display.displays.Length}");
            if (Display.displays.Length > 1)
                Display.displays[1].Activate();
        }

        private void SetCameraProjectionMatrix()
        {
            var projParam = clothCorrectCameraUI.CamProj;
            if (projParam.x <= projParam.y || projParam.z <= projParam.w)
            {
                clothCorrectCameraUI.CamProj = _prevProjParam;
                return;
            }

            var proj = clothCorrectCameraUI.UseFrustum
                ? Matrix4x4.Frustum(
                    projParam.w, projParam.z, projParam.y, projParam.x,
                    correctCamera.nearClipPlane, correctCamera.farClipPlane
                )
                : Matrix4x4.Perspective(
                    clothCorrectCameraUI.Fov, clothCorrectCameraUI.Aspect,
                    correctCamera.nearClipPlane, correctCamera.farClipPlane
                );
            correctCamera.projectionMatrix = proj;
            _prevProjParam = projParam;
        }
    }
}