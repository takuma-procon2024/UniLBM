using System;
using System.Collections;
using UnityEngine;

namespace Solver
{
    [RequireComponent(typeof(LbmSolverBehaviour))]
    public class LbmDebugObstacleViewer : MonoBehaviour
    {
        private static readonly int SizePropId = Shader.PropertyToID("size");
        private static readonly int lineColorPropId = Shader.PropertyToID("line_color");
        private static readonly int fieldPropId = Shader.PropertyToID("field");
        private static readonly int cellResPropId = Shader.PropertyToID("cell_res");

        [SerializeField] private float size = 100;
        [SerializeField] private Color lineColor = Color.white;
        [SerializeField] private Material mat;

        private bool _isInitialized;
        private RenderParams _renderParams;
        private LbmSolverBehaviour _solver;
        private int _vertCount;

        private IEnumerator Start()
        {
            TryGetComponent(out _solver);
            yield return new WaitUntil(() => _solver.Solver != null);

            var cellSize = _solver.Solver.GetCellSize();
            _vertCount = (int)(cellSize * cellSize * cellSize);

            if (!mat) 
                mat = new Material(Shader.Find("Unlit/LBM_Obstacles"));
            mat.SetBuffer(fieldPropId, _solver.Solver.GetFieldBuffer());
            mat.SetInt(cellResPropId, (int)cellSize);

            _renderParams = new RenderParams(mat)
            {
                worldBounds = new Bounds
                {
                    min = Vector3.zero,
                    max = new Vector3(cellSize, cellSize, cellSize)
                }
            };

            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized) return;

            mat.SetFloat(SizePropId, size);
            mat.SetColor(lineColorPropId, lineColor);

            Graphics.RenderPrimitives(_renderParams, MeshTopology.Points, _vertCount);
        }
    }
}