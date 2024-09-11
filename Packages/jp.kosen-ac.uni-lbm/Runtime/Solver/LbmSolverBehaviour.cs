using Effector.Impl;
using Solver.Impls;
using Unity.Mathematics;
using UnityEngine;

namespace Solver
{
    public class LbmSolverBehaviour : MonoBehaviour
    {
        [SerializeField] private ComputeShader lbmShader, effectorShader;
        [SerializeField] private Material effectorMaterial;
        [SerializeField] private uint width = 50;
        [SerializeField] private uint height = 50;
        [SerializeField] private uint depth = 50;
        [SerializeField] private float tau = 0.91f;
        [SerializeField] private Vector3 force = new(0.0002f, 0, 0);
        private PointEffector _effector;

        private UniLbmSolverBase _solver;

        private void Start()
        {
            _solver = new ComputeShaderSolver3D(lbmShader, new uint3(width, height, depth), tau, force);
            _effector = new PointEffector(new uint3(width, height, depth), 8000, effectorShader, effectorMaterial,
                _solver.GetFieldBuffer(), _solver.GetVelocityBuffer());
        }

        private void Update()
        {
            _solver.Step();
            _effector.Update();
        }

        private void OnDestroy()
        {
            _solver.Dispose();
            _effector.Dispose();

            _solver = null;
            _effector = null;
        }
    }
}