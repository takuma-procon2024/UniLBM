using Solver.Impls;
using Unity.Mathematics;
using UnityEngine;

namespace Solver
{
    public class LbmSolverBehaviour : MonoBehaviour
    {
        [SerializeField] private ComputeShader lbmShader;
        [SerializeField] private uint width = 50;
        [SerializeField] private uint height = 50;
        [SerializeField] private uint depth = 50;
        [SerializeField] private float tau = 0.91f;
        [SerializeField] private Vector3 force = new(0.0002f, 0, 0);

        private UniLbmSolverBase _solver;

        private void Start()
        {
            _solver = new ComputeShaderSolver3D(lbmShader, new uint3(width, height, depth), tau, force);
        }

        private void Update()
        {
            _solver.Step();
        }

        private void OnDestroy()
        {
            _solver.Dispose();
        }
    }
}