using TriInspector;
using UI;
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
        private ClothTofSensorManager _clothTofSensorManager;
        private LbmForceSourceManager _forceSourceManager;
        private ILbmSolver _lbmSolver;
        private LbmObstacles _obstacles;
        private LbmParticle _particle;

        private void Initialize()
        {
            _clothSolver = new ClothSolver(clothShader, new int2(clothResolution), GetClothData());
            _lbmSolver = new D3Q19LbmSolver(lbmShader, _clothSolver, cellResolution, new D3Q19LbmSolver.Data
            {
                Tau = tau
            });
            _particle = new LbmParticle(particleShader, _lbmSolver, particleMaterial, oneSideParticleNum,
                new LbmParticle.Data
                {
                    ParticleSpeed = particleSpeed,
                    MaxLifetime = maxLifetime,
                    ParticleLayer = particleLayer
                });
            ClothRenderer.Initialize(clothMaterial, clothRenderGo, _clothSolver);
            if (isEnableUnlitCloth)
                UnlitClothRenderer.Initialize(unlitClothMaterial, unlitClothRenderGo, clothRenderGo, _clothSolver);
            _clothLbm = new ClothLbmIntegrator(clothLbmShader, _clothSolver, _lbmSolver, _particle,
                GetClothLbmIntegratorData());
            _forceSourceManager =
                new LbmForceSourceManager(forceSourceShader, _lbmSolver, _particle, forceSourceRoot);
            _clothTofSensorManager = new ClothTofSensorManager(tofSensorShader, tofSensorRoot, _clothSolver,
                GetClothTofSensorData());
            _obstacles = new LbmObstacles(obstacleMaterial, _lbmSolver, particleLayer);
        }

        private void Simulate()
        {
            _clothLbm.SetData(GetClothLbmIntegratorData());
            _particle.SetData(new LbmParticle.Data
            {
                ParticleSpeed = particleSpeed,
                MaxLifetime = maxLifetime
            });
            if (isEnableTofSensor) _clothTofSensorManager.SetData(GetClothTofSensorData());

            // IMPORTANT: ここの順番大事!!
            _lbmSolver.ResetField();
            _clothLbm.Reset();

            _clothLbm.Update();
            if (isEnableForceSource) _forceSourceManager.Update();
            if (isEnableTofSensor) _clothTofSensorManager.Update();
            _lbmSolver.Update();
            _clothSolver.Update();
            _particle.Update(1 / 60f);
            if (isDrawObstacles) _obstacles.Update();
        }

        #region Ingame Debug

        [Title("InGame Debug")] [SerializeField]
        private InGameDebugWindow inGameDebugWindow;

        [SerializeField] private bool isEnableInGameDebug;
        public bool IsEnableInGameDebug => isEnableInGameDebug;

        private void AddFieldToInGameDebugUI()
        {
            if (!isEnableInGameDebug) return;

            var win = inGameDebugWindow;

            win.AddField("ParticleSpeed", particleSpeed);
            win.AddField("MaxLifetime", maxLifetime);
            win.AddField("DrawObstacle", isDrawObstacles);
            win.AddField("ClothVelScale", velocityScale);
            win.AddField("ClothMaxVel", clothMaxVelocity);
            win.AddField("ForceSource", isEnableForceSource);
            win.AddField("ToF Sensor", isEnableTofSensor);
            win.AddField("ToFDefaultDistance", tofDefaultDistance);
            win.AddField("TofForceScale", tofForceScale);
            win.AddField("TofDistanceScale", tofDistanceScale);
        }

        private void ApplyInGameDataToGameObj()
        {
            if (!isEnableInGameDebug) return;
            var win = inGameDebugWindow;

            if (win.TryGetField("ParticleSpeed", out float pSpeed))
                particleSpeed = pSpeed;
            if (win.TryGetField("MaxLifetime", out float maxLife))
                maxLifetime = maxLife;
            if (win.TryGetField("DrawObstacle", out bool drawObstacle))
                isDrawObstacles = drawObstacle;
            if (win.TryGetField("ClothVelScale", out float velScale))
                velocityScale = velScale;
            if (win.TryGetField("ClothMaxVel", out float maxVel))
                clothMaxVelocity = maxVel;
            if (win.TryGetField("ForceSource", out bool forceSource))
                isEnableForceSource = forceSource;
            if (win.TryGetField("ToF Sensor", out bool tofSensor))
                isEnableTofSensor = tofSensor;
            if (win.TryGetField("ToFDefaultDistance", out float tofDist))
                tofDefaultDistance = tofDist;
            if (win.TryGetField("TofForceScale", out float tofFScale))
                tofForceScale = tofFScale;
            if (win.TryGetField("TofDistanceScale", out float tofDScale))
                tofDistanceScale = tofDScale;
        }

        #endregion

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
                MaxExtVelocity = clothMaxVelocity,
                DeltaTime = deltaTime,
                VerletIteration = verletIteration,
                MaxForce = clothMaxForce
            };
        }

        private ClothLbmIntegrator.Data GetClothLbmIntegratorData()
        {
            var childTrans = clothRenderGo.transform;
            return new ClothLbmIntegrator.Data
            {
                LbmCellSize = clothLbmCellSize,
                Transform = childTrans.localToWorldMatrix
            };
        }

        private ClothTofSensorManager.Data GetClothTofSensorData()
        {
            var childTrans = clothRenderGo.transform;
            return new ClothTofSensorManager.Data
            {
                TofRadius = tofRadius,
                ClothTransform = childTrans.localToWorldMatrix,
                TofDefaultDistance = tofDefaultDistance,
                ForceScale = tofForceScale,
                DistanceScale = tofDistanceScale
            };
        }

        #endregion

        #region Unity Callback

        private void Start()
        {
            Initialize();
            AddFieldToInGameDebugUI();
        }

        private void Update()
        {
            Simulate();
            ApplyInGameDataToGameObj();
        }

        private void OnDestroy()
        {
            _lbmSolver.Dispose();
            _particle.Dispose();
            _clothSolver.Dispose();
            _forceSourceManager?.Dispose();
            _clothTofSensorManager?.Dispose();
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
        [SerializeField] private int particleLayer;

        [Title("Obstacles")] [SerializeField] private Material obstacleMaterial;
        [SerializeField] private bool isDrawObstacles = true;

        [Title("Cloth")] [SerializeField] private ComputeShader clothShader;
        [SerializeField] private Material clothMaterial;
        [SerializeField] private uint2 clothResolution = new(128, 128);
        [SerializeField] private float deltaTime = 1 / 60f;
        [Range(1, 256)] [SerializeField] private int verletIteration = 4;
        [SerializeField] private float restLength = 0.02f;
        [SerializeField] private float stiffness = 10000f;
        [SerializeField] private float damping = 0.996f;
        [SerializeField] private float mass = 1.0f;
        [SerializeField] private float3 gravity = new(0, -9.81f, 0);
        [SerializeField] private float velocityScale = 1.0f;
        [SerializeField] private float clothMaxVelocity = 1000;
        [SerializeField] private float clothMaxForce = 300;

        [Title("Cloth LBM Integration")] [SerializeField]
        private ComputeShader clothLbmShader;

        [SerializeField] private GameObject clothRenderGo;
        [SerializeField] private float clothLbmCellSize = 1;

        [Title("Force Source")] [SerializeField]
        private ComputeShader forceSourceShader;

        [SerializeField] private GameObject forceSourceRoot;
        [SerializeField] private bool isEnableForceSource;

        [Title("ToF Sensor")] [SerializeField] private ComputeShader tofSensorShader;
        [SerializeField] private GameObject tofSensorRoot;
        [SerializeField] private float tofRadius = 2f;
        [SerializeField] private float tofDefaultDistance = 20f;
        [SerializeField] private float tofForceScale = 1;
        [SerializeField] private float tofDistanceScale = 0.005f;
        [SerializeField] private bool isEnableTofSensor;

        [Title("Unlit Cloth")] [SerializeField]
        private bool isEnableUnlitCloth;

        [SerializeField] private Material unlitClothMaterial;
        [SerializeField] private GameObject unlitClothRenderGo;

        #endregion
    }
}