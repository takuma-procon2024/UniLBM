using System.Linq;
using Projection;
using UI.Parts;
using Unity.Mathematics;
using UnityEngine;

namespace UI.Homography
{
    public class HomographyImageUI : MonoBehaviour
    {
        private static readonly int MatrixPropId = Shader.PropertyToID("_HomographyMatrix");
        private static readonly int InvMatrixPropId = Shader.PropertyToID("_HomographyInvMatrix");

        [SerializeField] private DraggableHandle[] handles;
        [SerializeField] private Material homographyMat, screenHomographyMat;
        [SerializeField] private int targetScreen;

        private DraggableHandle p00, p01, p10, p11;

        private void Start()
        {
            TryGetComponent(out RectTransform rectTransform);

            p00 = handles.First(v => v.PosType == DraggableHandle.Pos.P00);
            p01 = handles.First(v => v.PosType == DraggableHandle.Pos.P01);
            p10 = handles.First(v => v.PosType == DraggableHandle.Pos.P10);
            p11 = handles.First(v => v.PosType == DraggableHandle.Pos.P11);

            var screenRes = Screen.resolutions[targetScreen];
            var aspect = screenRes.width / (float)screenRes.height;

            rectTransform.sizeDelta = new Vector2(aspect * rectTransform.sizeDelta.y, rectTransform.sizeDelta.y);
        }

        private void Update()
        {
            var matrix = HomographyMatrix.CalcHomographyMatrix(
                p00.NormalizedPos, p01.NormalizedPos,
                p10.NormalizedPos, p11.NormalizedPos
            );
            var invMatrix = math.inverse(matrix);

            homographyMat.SetMatrix(MatrixPropId, matrix);
            homographyMat.SetMatrix(InvMatrixPropId, invMatrix);
            screenHomographyMat.SetMatrix(MatrixPropId, matrix);
            screenHomographyMat.SetMatrix(InvMatrixPropId, invMatrix);
        }
    }
}