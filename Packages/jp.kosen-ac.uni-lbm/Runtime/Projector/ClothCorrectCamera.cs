using UI;
using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Projector
{
    public class ClothCorrectCamera : MonoBehaviour
    {
        [SerializeField] private Camera correctCamera;
        [SerializeField] private ClothCorrectCameraUI clothCorrectCameraUI;

        private float4 _prevProjParam;

        private void Start()
        {
            var unlitCloth = FindAnyObjectByType<UnlitCloth>();
            if (unlitCloth == null) Debug.LogError("UnlitCloth is not found.");

            clothCorrectCameraUI.gameObject.SetActive(true);
            clothCorrectCameraUI.Initialize(correctCamera.transform, unlitCloth.transform, new float4(1, -1, 1, -1),
                correctCamera.fieldOfView, correctCamera.aspect);

            unlitCloth.Initialize(clothCorrectCameraUI);
        }

        private void Update()
        {
            correctCamera.transform.SetPositionAndRotation(
                clothCorrectCameraUI.CamPos.xyz,
                Quaternion.Euler(clothCorrectCameraUI.CamRot.xyz)
            );
            SetCameraProjectionMatrix();
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