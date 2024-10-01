using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using UniLbm.Common;
using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Cloth
{
    public class ClothSolver : IDisposable
    {
        private readonly int2 _res;
        private readonly ComputeShaderWrapper<Kernels, Uniforms> _shader;
        private int _verletIteration;

        public ClothSolver(ComputeShader shader, in int2 res, in Data data)
        {
            _res = res;
            _shader = new ComputeShaderWrapper<Kernels, Uniforms>(shader);

            InitBuffers();
            SetBuffers();
            SetData(in data);

            ResetBuffer();
        }

        public int2 ClothResolution => _res;

        public void Dispose()
        {
            _positionBuffer[0].Release();
            _positionBuffer[1].Release();
            _prevPosBuffer[0].Release();
            _prevPosBuffer[1].Release();
            NormalBuffer.Release();
            ExternalForceBuffer.Release();
        }

        public void Update()
        {
            for (var i = 0; i < _verletIteration; i++)
            {
                _shader.Dispatch(Kernels.simulation, new uint3(new uint2(_res), 1));
                SwapBuffers();
            }
        }

        #region Debugs

        [Conditional("UNITY_EDITOR")]
        public void DrawSimulationBufferOnGui()
        {
            var rw = (int)math.round(_res.x * 1);
            var rh = (int)math.round(_res.y * 1);

            var storeColor = GUI.color;
            GUI.color = Color.gray;

            var r00 = new Rect(rw * 0, rh * 0, rw, rh);
            var r01 = new Rect(rw * 1, rh * 0, rw, rh);
            var r10 = new Rect(rw * 0, rh * 1, rw, rh);
            var r11 = new Rect(rw * 1, rh * 1, rw, rh);
            var r20 = new Rect(rw * 0, rh * 2, rw, rh);

            GUI.DrawTexture(r00, _positionBuffer[0]);
            GUI.DrawTexture(r01, _positionBuffer[1]);
            GUI.DrawTexture(r10, _prevPosBuffer[0]);
            GUI.DrawTexture(r11, _prevPosBuffer[1]);
            GUI.DrawTexture(r20, NormalBuffer);

            GUI.Label(r00, "Position Buffer 0");
            GUI.Label(r01, "Position Buffer 1");
            GUI.Label(r10, "Prev Position Buffer 0");
            GUI.Label(r11, "Prev Position Buffer 1");
            GUI.Label(r20, "Normal Buffer");

            GUI.color = storeColor;
        }

        #endregion

        #region ComputeShader

        private RenderTexture[] _positionBuffer;
        private RenderTexture[] _prevPosBuffer;

        public RenderTexture PositionBuffer => _positionBuffer[0];
        public RenderTexture NormalBuffer { get; private set; }

        public RenderTexture ExternalForceBuffer { get; private set; }

        private void InitBuffers()
        {
            _positionBuffer = new RenderTexture[2];
            _prevPosBuffer = new RenderTexture[2];
            for (var i = 0; i < _positionBuffer.Length; i++)
            {
                _positionBuffer[i] = RenderTexUtil.Create2D(in _res);
                _prevPosBuffer[i] = RenderTexUtil.Create2D(in _res);
            }

            NormalBuffer = RenderTexUtil.Create2D(in _res);
            ExternalForceBuffer = RenderTexUtil.Create2D(in _res);
        }

        private void SetBuffers()
        {
            var allKernels = new[] { Kernels.init, Kernels.simulation };

            _shader.SetTexture(allKernels, Uniforms.pos_prev_buffer_out, _prevPosBuffer[1]);
            _shader.SetTexture(allKernels, Uniforms.pos_curr_buffer_out, _positionBuffer[1]);
            _shader.SetTexture(allKernels, Uniforms.normal_buffer_out, NormalBuffer);

            _shader.SetTexture(Kernels.simulation, Uniforms.pos_prev_buffer, _prevPosBuffer[0]);
            _shader.SetTexture(Kernels.simulation, Uniforms.pos_curr_buffer, _positionBuffer[0]);
            _shader.SetTexture(Kernels.simulation, Uniforms.external_force_buffer, ExternalForceBuffer);
        }

        private void SetData(in Data data)
        {
            var totalClothLength = new float2(
                _res.x * data.RestLength,
                _res.y * data.RestLength
            );
            _verletIteration = data.VerletIteration;

            _shader.SetVector(Uniforms.total_cloth_length, new float4(totalClothLength, 0, 0));
            _shader.SetFloat(Uniforms.rest_length, data.RestLength);
            _shader.SetFloat(Uniforms.stiffness, data.Stiffness);
            _shader.SetFloat(Uniforms.damp, data.Damping);
            _shader.SetFloat(Uniforms.inv_mass, 1.0f / data.Mass);
            _shader.SetVector(Uniforms.gravity, new float4(data.Gravity, 0));
            _shader.SetFloat(Uniforms.velocity_scale, data.VelocityScale);
            _shader.SetFloat(Uniforms.dt, data.DeltaTime / data.VerletIteration);

            _shader.SetInts(Uniforms.cloth_resolution, _res.x, _res.y);
        }

        private void SwapBuffers()
        {
            (_positionBuffer[0], _positionBuffer[1]) = (_positionBuffer[1], _positionBuffer[0]);
            (_prevPosBuffer[0], _prevPosBuffer[1]) = (_prevPosBuffer[1], _prevPosBuffer[0]);

            _shader.SetTexture(Kernels.simulation, Uniforms.pos_prev_buffer, _prevPosBuffer[0]);
            _shader.SetTexture(Kernels.simulation, Uniforms.pos_curr_buffer, _positionBuffer[0]);
            _shader.SetTexture(Kernels.simulation, Uniforms.pos_prev_buffer_out, _prevPosBuffer[1]);
            _shader.SetTexture(Kernels.simulation, Uniforms.pos_curr_buffer_out, _positionBuffer[1]);
        }
        
        private void ResetBuffer()
        {
            _shader.Dispatch(Kernels.init, new uint3(new uint2(_res), 1));
            Graphics.Blit(_positionBuffer[1], _positionBuffer[0]);
            Graphics.Blit(_prevPosBuffer[1], _prevPosBuffer[0]);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Kernels
        {
            init,
            simulation
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Uniforms
        {
            pos_prev_buffer,
            pos_curr_buffer,
            pos_prev_buffer_out,
            pos_curr_buffer_out,
            normal_buffer_out,
            external_force_buffer,
            cloth_resolution,
            total_cloth_length,
            rest_length,
            gravity,
            stiffness,
            damp,
            inv_mass,
            dt,

            velocity_scale
        }

        public readonly struct Data
        {
            public float RestLength { get; init; }
            public float Stiffness { get; init; }
            public float Damping { get; init; }
            public float Mass { get; init; }
            public float3 Gravity { get; init; }
            public float VelocityScale { get; init; }
            public float DeltaTime { get; init; }
            public int VerletIteration { get; init; }
        }

        #endregion
    }
}