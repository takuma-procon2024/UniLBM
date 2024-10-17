using System.Diagnostics.CodeAnalysis;
using TriInspector;
using UI;
using UnityEngine;

namespace UniLbm.Common
{
    [RequireComponent(typeof(UniSimulator))]
    public class UniMaterialDebugUI : MonoBehaviour
    {
        [SerializeField] private InGameDebugWindow inGameDebugWindow;
        [Title("Resources")] [SerializeField] private Material obstacleMat, particleMat, clothMat;
        private MaterialWrapper<ClothProp> _clothMatWrapper;
        private MaterialWrapper<ObstacleProp> _obstacleMatWrapper;
        private MaterialWrapper<ParticleProp> _particleMatWrapper;

        private UniSimulator _simulator;

        private void Start()
        {
            TryGetComponent(out _simulator);

            if (!_simulator.IsEnableInGameDebug) return;

            _obstacleMatWrapper = new MaterialWrapper<ObstacleProp>(obstacleMat);
            _particleMatWrapper = new MaterialWrapper<ParticleProp>(particleMat);
            _clothMatWrapper = new MaterialWrapper<ClothProp>(clothMat);

            var win = inGameDebugWindow;
            win.AddField("§ Materials");
            win.AddField("ObstacleVelScale", _obstacleMatWrapper.GetFloat(ObstacleProp.velocity_scale));
            win.AddField("ParticleMinVel", _particleMatWrapper.GetFloat(ParticleProp.min_velocity));
            win.AddField("ParticleMaxVel", _particleMatWrapper.GetFloat(ParticleProp.max_velocity));
            win.AddField("ParticleHueSpeed", _particleMatWrapper.GetFloat(ParticleProp.hue_speed));
            win.AddField("ParticleLength", _particleMatWrapper.GetFloat(ParticleProp.particle_length));
            win.AddField("ParticleWidth", _particleMatWrapper.GetFloat(ParticleProp.particle_width));
            win.AddField("ClothExtForceMul", _clothMatWrapper.GetFloat(ClothProp.external_force_mul));
            win.AddField("ClothNormalScale", _clothMatWrapper.GetFloat(ClothProp.normal_scale));
            win.AddField("ShowExternalForce", _clothMatWrapper.GetBool(ClothShowExternalForce));
        }

        private void Update()
        {
            if (!_simulator.IsEnableInGameDebug) return;

            var win = inGameDebugWindow;
            if (win.TryGetField("ObstacleVelScale", out float obstacleVelScale))
                _obstacleMatWrapper.SetFloat(ObstacleProp.velocity_scale, obstacleVelScale);
            if (win.TryGetField("ParticleMinVel", out float particleMinVel))
                _particleMatWrapper.SetFloat(ParticleProp.min_velocity, particleMinVel);
            if (win.TryGetField("ParticleMaxVel", out float particleMaxVel))
                _particleMatWrapper.SetFloat(ParticleProp.max_velocity, particleMaxVel);
            if (win.TryGetField("ParticleHueSpeed", out float particleHueSpeed))
                _particleMatWrapper.SetFloat(ParticleProp.hue_speed, particleHueSpeed);
            if (win.TryGetField("ParticleLength", out float particleLength))
                _particleMatWrapper.SetFloat(ParticleProp.particle_length, particleLength);
            if (win.TryGetField("ParticleWidth", out float particleWidth))
                _particleMatWrapper.SetFloat(ParticleProp.particle_width, particleWidth);
            if (win.TryGetField("ClothExtForceMul", out float clothExtForceMul))
                _clothMatWrapper.SetFloat(ClothProp.external_force_mul, clothExtForceMul);
            if (win.TryGetField("ClothNormalScale", out float clothNormalScale))
                _clothMatWrapper.SetFloat(ClothProp.normal_scale, clothNormalScale);
            if (win.TryGetField("ShowExternalForce", out bool showExternalForce))
                _clothMatWrapper.SetBool(ClothShowExternalForce, showExternalForce);
        }

        #region Materials

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum ObstacleProp
        {
            velocity_scale
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum ParticleProp
        {
            min_velocity,
            max_velocity,
            hue_speed,
            particle_length,
            particle_width
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum ClothProp
        {
            external_force_mul,
            normal_scale
        }

        private const string ClothShowExternalForce = "_SHOW_EXTERNAL_FORCE";

        #endregion
    }
}