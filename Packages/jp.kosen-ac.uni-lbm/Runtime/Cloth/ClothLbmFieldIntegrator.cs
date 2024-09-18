using Solver;
using Unity.Mathematics;
using UnityEngine;

namespace Cloth
{
    public class ClothLbmFieldIntegrator : MonoBehaviour
    {
        [SerializeField] private LbmSolverBehaviour lbmSolver;
        [SerializeField] private uint3 offset;
        [SerializeField] private float3 scale;
        private uint _cellSize;

        private GraphicsBuffer _fieldBuffer;

        private void Start()
        {
            _fieldBuffer = lbmSolver.Solver.GetFieldBuffer();
        }
    }
}