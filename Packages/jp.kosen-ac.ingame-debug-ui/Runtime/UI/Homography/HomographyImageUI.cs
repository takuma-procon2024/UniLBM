using System.Linq;
using Projection;
using Unity.Mathematics;
using UnityEngine;

namespace UI.Homography
{
    public class HomographyImageUI : MonoBehaviour
    {
        private static readonly int MatrixPropId = Shader.PropertyToID("_HomographyMatrix");
        private static readonly int InvMatrixPropId = Shader.PropertyToID("_HomographyInvMatrix");

        [SerializeField] private DraggableHandle[] handles;
        [SerializeField] private InGameDebugWindow inGameDebugWindow;
        [SerializeField] private Material screenHomographyMat;
        [SerializeField] private int targetScreen;

        private DraggableHandle _p00, _p01, _p10, _p11;
        private int2 _screenRes;

        private void Update()
        {
            var matrix = HomographyMatrix.CalcHomographyMatrix(
                _p00.NormalizedPos, _p01.NormalizedPos,
                _p10.NormalizedPos, _p11.NormalizedPos
            );
            var invMatrix = math.inverse(matrix);

            screenHomographyMat.SetMatrix(MatrixPropId, matrix);
            screenHomographyMat.SetMatrix(InvMatrixPropId, invMatrix);
            SaveHandlePos();
        }

        public void Initialize()
        {
            TryGetComponent(out RectTransform rectTransform);

            _p00 = handles.First(v => v.PosType == DraggableHandle.Pos.P00);
            _p01 = handles.First(v => v.PosType == DraggableHandle.Pos.P01);
            _p10 = handles.First(v => v.PosType == DraggableHandle.Pos.P10);
            _p11 = handles.First(v => v.PosType == DraggableHandle.Pos.P11);

            var screenRes = Screen.resolutions[targetScreen];
            var aspect = screenRes.width / (float)screenRes.height;
            rectTransform.sizeDelta = new Vector2(aspect * rectTransform.sizeDelta.y, rectTransform.sizeDelta.y);

            var dataStore = inGameDebugWindow.DataStore;
            if (dataStore.TryGetData("ScreenAspect", out float storeAspect)
                && Mathf.Approximately(storeAspect, aspect))
                // アスペクト比が変わっていない場合は保存していたハンドル位置を復元
                LoadAndSetHandlePos();
            else
                // 画面解像度が変わっている場合はハンドル位置を初期化
                foreach (var handle in handles)
                    handle.MoveDefaultPos();

            // アスペクト比を保存
            dataStore.SetData("ScreenAspect", aspect);

            // 無理やりUpdateを呼ぶ
            Update();
        }

        private void LoadAndSetHandlePos()
        {
            var dataStore = inGameDebugWindow.DataStore;

            if (!dataStore.TryGetData("p00_01", out float4 p00And01)
                || !dataStore.TryGetData("p10_11", out float4 p10And11))
            {
                foreach (var handle in handles)
                    handle.MoveDefaultPos();
                return;
            }

            _p00.NormalizedPos = p00And01.xy;
            _p01.NormalizedPos = p00And01.zw;
            _p10.NormalizedPos = p10And11.xy;
            _p11.NormalizedPos = p10And11.zw;
        }

        private void SaveHandlePos()
        {
            var dataStore = inGameDebugWindow.DataStore;
            dataStore.SetData("p00_01", new float4(_p00.NormalizedPos, _p01.NormalizedPos));
            dataStore.SetData("p10_11", new float4(_p10.NormalizedPos, _p11.NormalizedPos));
        }
    }
}