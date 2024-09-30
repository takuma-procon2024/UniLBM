using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UniLbm.Common;
using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Lbm
{
    /// <summary>
    ///     LBMのパーティクルの制御をするクラス
    /// </summary>
    public class LbmParticle : IDisposable
    {
        private static readonly int sizePropId = Shader.PropertyToID("size");
        private static readonly int particlesPropId = Shader.PropertyToID("particles");

        private readonly int _dispatchSize;
        private readonly ILbmSolver _lbmSolver;
        private readonly int _particleNum, _oneSideParticleNum;
        private readonly RenderParams _renderParams;
        private readonly ComputeShaderWrapper<Kernels, Uniforms> _shader;

        public LbmParticle(ComputeShader shader, ILbmSolver lbmSolver, Material mat, uint oneSideParticleNum,
            in Data data)
        {
            _shader = new ComputeShaderWrapper<Kernels, Uniforms>(shader);
            _lbmSolver = lbmSolver;
            _oneSideParticleNum = (int)oneSideParticleNum;
            _particleNum = (int)(oneSideParticleNum * oneSideParticleNum * oneSideParticleNum);

            InitBuffers((int)oneSideParticleNum);
            SetBuffers();
            SetData(in data);

            var matSize = mat.GetFloat(sizePropId);
            _renderParams = new RenderParams(mat)
            {
                worldBounds = new Bounds
                {
                    min = Vector3.zero,
                    max = new Vector3(matSize, matSize, matSize)
                }
            };
            mat.SetBuffer(particlesPropId, _particlesBuffer);

            _shader.Dispatch(Kernels.init_particle, (uint)_oneSideParticleNum);
        }

        public void Dispose()
        {
            _particlesBuffer?.Dispose();
        }

        public void Update(float deltaTime)
        {
            _shader.SetFloat(Uniforms.delta_time, 1 / 60f);
            _shader.Dispatch(Kernels.update_particle, (uint)_oneSideParticleNum);

            Graphics.RenderPrimitives(_renderParams, MeshTopology.Points, _particleNum);
        }

        #region ComputeShader

        private GraphicsBuffer _particlesBuffer;

        private void InitBuffers(int oneSideParticleNum)
        {
            var particleNum = oneSideParticleNum * oneSideParticleNum * oneSideParticleNum;
            _particlesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleNum,
                Marshal.SizeOf<ParticleData>());
        }

        private void SetBuffers()
        {
            _shader.SetBuffer(new[] { Kernels.init_particle, Kernels.update_particle }, Uniforms.particles,
                _particlesBuffer);
            _shader.SetBuffer(Kernels.update_particle, Uniforms.vel_dens, _lbmSolver.VelDensBuffer);
            _shader.SetBuffer(Kernels.update_particle, Uniforms.field, _lbmSolver.FieldBuffer);
        }

        private void SetData(in Data data)
        {
            _shader.SetFloat(Uniforms.particle_speed, data.ParticleSpeed);
            _shader.SetFloat(Uniforms.max_lifetime, data.MaxLifetime);
            _shader.SetInt(Uniforms.cell_res, _lbmSolver.CellRes);
            _shader.SetInt(Uniforms.one_side_particle_num, _oneSideParticleNum);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Kernels
        {
            init_particle,
            update_particle
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Uniforms
        {
            delta_time,
            particle_speed,
            cell_res,
            one_side_particle_num,
            max_lifetime,
            field,
            vel_dens,
            particles
        }

        public readonly struct Data
        {
            public float ParticleSpeed { get; init; }
            public float MaxLifetime { get; init; }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ParticleData
        {
            public float4 pos_lifetime;
            public float4 prev_pos_vel;
        }

        #endregion
    }
}