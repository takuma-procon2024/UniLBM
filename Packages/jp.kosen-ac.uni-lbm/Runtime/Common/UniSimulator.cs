using TriInspector;
using UniLbm.Lbm;
using UnityEngine;

namespace UniLbm.Common
{
    /// <summary>
    ///     シミュレーションの管理を行うクラス
    /// </summary>
    public class UniSimulator : MonoBehaviour
    {
        private ILbmSolver _lbmSolver;
        private LbmObstacles _obstacles;
        private LbmParticle _particle;

        private void Initialize()
        {
            _lbmSolver = new D3Q19LbmSolver(lbmShader, cellResolution, new D3Q19LbmSolver.Data
            {
                Tau = tau
            });
            _particle = new LbmParticle(particleShader, _lbmSolver, particleMaterial, oneSideParticleNum,
                new LbmParticle.Data
                {
                    ParticleSpeed = particleSpeed,
                    MaxLifetime = maxLifetime
                });
            _obstacles = new LbmObstacles(obstacleMaterial, _lbmSolver);
        }

        private void Simulate()
        {
            // TODO: リセットのタイミングは実験しながら考える
            _lbmSolver.ResetFieldVelocity();
            _lbmSolver.Update();
            _particle.Update(1 / 60f);
            _obstacles.Update();
        }

        #region Unity Callback

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            Simulate();
        }

        private void OnDestroy()
        {
            _lbmSolver.Dispose();
            _particle.Dispose();
        }

        #endregion

        #region Serialize Field

        [Title("LBM")] [SerializeField] private ComputeShader lbmShader;
        [SerializeField] private uint cellResolution = 128;
        [SerializeField] private float tau = 1.2f;

        [Title("Particle")] [SerializeField] private ComputeShader particleShader;
        [SerializeField] private Material particleMaterial;
        [SerializeField] private uint oneSideParticleNum = 100;
        [SerializeField] private float particleSpeed = 0.1f;
        [SerializeField] private float maxLifetime = 10f;

        [Title("Obstacles")] [SerializeField] private Material obstacleMaterial;

        #endregion
    }
}