using Projection;
using UI.Parts;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Homography
{
    [RequireComponent(typeof(Image))]
    public class HomographyImageUI : MonoBehaviour
    {
        private static readonly int MatrixPropId = Shader.PropertyToID("_HomographyMatrix");
        private static readonly int InvMatrixPropId = Shader.PropertyToID("_HomographyInvMatrix");

        [SerializeField] private DraggableHandle p00, p01, p10, p11;
        [SerializeField] private Material homographyMat;

        private void Update()
        {
            var matrix = HomographyMatrix.CalcHomographyMatrix(
                p00.NormalizedPos, p01.NormalizedPos,
                p10.NormalizedPos, p11.NormalizedPos
            );
            homographyMat.SetMatrix(MatrixPropId, matrix);
            homographyMat.SetMatrix(InvMatrixPropId, math.inverse(matrix));
        }
    }
}