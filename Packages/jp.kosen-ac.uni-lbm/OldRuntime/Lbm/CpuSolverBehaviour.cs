using Lbm.Impls;
using UnityEngine;

namespace Lbm
{
    public class CpuSolverBehaviour : MonoBehaviour
    {
        [SerializeField] private uint cellSize = 20;
        [SerializeField] private float tau = 0.6f;

        private CpuD3Q19Solver _solver;

        private void Start()
        {
            _solver = new CpuD3Q19Solver(cellSize, tau);
        }

        private void Update()
        {
            _solver.Step();
            _solver.DebugDraw();
        }
    }
}