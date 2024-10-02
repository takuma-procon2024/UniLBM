using TriInspector;
using UniLbm.Cloth;
using UniLbm.Lbm;
using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Common
{
    /// <summary>
    ///     シミュレーションの管理を行うクラス
    /// </summary>
    public class UniSimulator : MonoBehaviour
    {
        private ClothLbmIntegrator _clothLbm;

        private ClothSolver _clothSolver;
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
            _clothSolver = new ClothSolver(clothShader, new int2(clothResolution), GetClothData());
            ClothRenderer.Initialize(clothMaterial, clothRenderGo, _clothSolver);
            _clothLbm = new ClothLbmIntegrator(clothLbmShader, _clothSolver, _lbmSolver, new ClothLbmIntegrator.Data
            {
                LbmCellSize = clothLbmCellSize,
                Transform = clothRenderGo.transform.localToWorldMatrix
            });
        }

        private void Simulate()
        {
            _clothLbm.SetData(GetClothLbmData());

            // TODO: リセットのタイミングは実験しながら考える
            _lbmSolver.ResetFieldVelocity();
            _clothSolver.Update();
            _clothLbm.Update();

            _lbmSolver.Update();
            _particle.Update(1 / 60f);
            _obstacles.Update();
        }

        #region Util

        private ClothSolver.Data GetClothData()
        {
            return new ClothSolver.Data
            {
                RestLength = restLength,
                Stiffness = stiffness,
                Damping = damping,
                Mass = mass,
                Gravity = gravity,
                VelocityScale = velocityScale,
                DeltaTime = deltaTime,
                VerletIteration = verletIteration
            };
        }

        private ClothLbmIntegrator.Data GetClothLbmData()
        {
            var childTrans = clothRenderGo.transform;
            var trs = float4x4.TRS(
                childTrans.position,
                childTrans.rotation,
                childTrans.localScale
            );
            return new ClothLbmIntegrator.Data
            {
                LbmCellSize = clothLbmCellSize,
                Transform = trs
            };
        }

        #endregion

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

#if DEBUG
        private void OnGUI()
        {
            if (isClothDebug) _clothSolver.DrawSimulationBufferOnGui();
        }
#endif

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

        [Title("Cloth")] [SerializeField] private ComputeShader clothShader;
        [SerializeField] private Material clothMaterial;
        [SerializeField] private uint2 clothResolution = new(128, 128);
        [SerializeField] private float deltaTime = 1 / 60f;
        [Range(1, 16)] [SerializeField] private int verletIteration = 4;
        [SerializeField] private float restLength = 0.02f;
        [SerializeField] private float stiffness = 10000f;
        [SerializeField] private float damping = 0.996f;
        [SerializeField] private float mass = 1.0f;
        [SerializeField] private float3 gravity = new(0, -9.81f, 0);
        [SerializeField] private float velocityScale = 1.0f;
        [SerializeField] private bool isClothDebug;

        [Title("Cloth LBM Integration")] [SerializeField]
        private ComputeShader clothLbmShader;

        [SerializeField] private GameObject clothRenderGo;
        [SerializeField] private float clothLbmCellSize = 1;

        #endregion
    }
}