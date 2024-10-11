using UI;
using UnityEngine;

namespace UniLbm.Lbm
{
    public class ClothCorrectCamera : MonoBehaviour
    {
        [SerializeField] private Camera correctCamera;
        [SerializeField] private ClothCorrectCameraUI clothCorrectCameraUI;

        private void Update()
        {
            correctCamera.fieldOfView = clothCorrectCameraUI.Fov;
            correctCamera.transform.position = new Vector3(
                clothCorrectCameraUI.CamX,
                clothCorrectCameraUI.CamY,
                correctCamera.transform.position.z
            );
        }
    }
}