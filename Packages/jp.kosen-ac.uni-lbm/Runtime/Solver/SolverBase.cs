using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Solver
{
    public readonly struct GpuThreads
    {
        public readonly int X, Y, Z;

        public GpuThreads(uint x, uint y, uint z)
        {
            X = (int)x;
            Y = (int)y;
            Z = (int)z;
        }
    }

    internal static class ComputeKernelsExt
    {
        internal static string ToKernelName(this ComputeKernels self)
        {
            return self switch
            {
                ComputeKernels.AddSourceDensity => "add_source_density",
                ComputeKernels.DiffuseDensity => "diffuse_density",
                ComputeKernels.AdvectDensity => "advect_density",
                ComputeKernels.AdvectDensityFromExt => "advect_density_from_ext",
                ComputeKernels.SwapDensity => "swap_density",
                ComputeKernels.AddSourceVelocity => "add_source_velocity",
                ComputeKernels.DiffuseVelocity => "diffuse_velocity",
                ComputeKernels.AdvectVelocity => "advect_velocity",
                ComputeKernels.SwapVelocity => "swap_velocity",
                ComputeKernels.ProjectStep1 => "project_step1",
                ComputeKernels.ProjectStep2 => "project_step2",
                ComputeKernels.ProjectStep3 => "project_step3",
                ComputeKernels.Draw => "draw",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public enum ComputeKernels
    {
        AddSourceDensity,
        DiffuseDensity,
        AdvectDensity,
        AdvectDensityFromExt,
        SwapDensity,

        AddSourceVelocity,
        DiffuseVelocity,
        AdvectVelocity,
        SwapVelocity,
        ProjectStep1,
        ProjectStep2,
        ProjectStep3,

        Draw
    }

    // ReSharper disable once InconsistentNaming
    internal static class DirectCompute5_0
    {
        public const int MaxThread = 1024;
        public const int MaxX = 1024;
        public const int MaxY = 1024;
        public const int MaxZ = 64;
        public const int MaxDispatch = 65535;
        public const int MaxProcess = MaxDispatch * MaxThread;
    }

    public abstract class SolverBase : MonoBehaviour
    {
        #region release

        private void CleanUp()
        {
            ReleaseRenderTexture(SolverTex);
            ReleaseRenderTexture(DensityTex);
            ReleaseRenderTexture(VelocityTex);
            ReleaseRenderTexture(PrevTex);

#if UNITY_EDITOR
            Debug.Log("Buffer released");
#endif
        }

        #endregion

        #region Variables

        protected Dictionary<ComputeKernels, int> KernelMap = new();
        protected GpuThreads GPUThreads;
        protected RenderTexture SolverTex;
        protected RenderTexture DensityTex;
        protected RenderTexture VelocityTex;
        protected RenderTexture PrevTex;
        private const string SolverProp = "solver";
        private const string DensityProp = "density";
        private const string VelocityProp = "velocity";
        private const string PrevProp = "prev";
        private const string SourceProp = "source";
        private const string DiffProp = "diff";
        private const string ViscProp = "visc";
        private const string DTProp = "dt";
        private const string VelocityCoefProp = "velocity_coef";
        private const string DensityCoefProp = "density_coef";

        protected int SolverId,
            DensityId,
            VelocityId,
            PrevId,
            SourceId,
            DiffId,
            ViscId,
            DTId,
            VelocityCoefId,
            DensityCoefId,
            SolverTexId;

        protected int Width, Height;


        [SerializeField] protected ComputeShader computeShader;

        [SerializeField] protected string solverTexProp = "SolverTex";

        [SerializeField] protected float diff;

        [SerializeField] protected float visc;

        [SerializeField] protected float velocityCoef;

        [SerializeField] protected float densityCoef;

        [SerializeField] protected bool isDensityOnly;

        [SerializeField] protected int lod;

        [SerializeField] protected bool debug;

        [SerializeField] protected Material debugMat;

        [SerializeField] private RenderTexture sourceTex;

        public RenderTexture SourceTex
        {
            set => sourceTex = value;
            get => sourceTex;
        }

        #endregion

        #region unity builtin

        protected virtual void Start()
        {
            Initialize();
        }

        protected virtual void Update()
        {
            if (Width != Screen.width || Height != Screen.height) InitializeComputeShader();
            computeShader.SetFloat(DiffId, diff);
            computeShader.SetFloat(ViscId, visc);
            computeShader.SetFloat(DiffId, diff);
            computeShader.SetFloat(ViscId, visc);
            computeShader.SetFloat(DTId, Time.deltaTime);
            computeShader.SetFloat(VelocityCoefId, velocityCoef);
            computeShader.SetFloat(DensityCoefId, densityCoef);

            if (!isDensityOnly) VelocityStep();
            DensityStep();

            computeShader.SetTexture(KernelMap[ComputeKernels.Draw], DensityId, DensityTex);
            computeShader.SetTexture(KernelMap[ComputeKernels.Draw], VelocityId, VelocityTex);
            computeShader.SetTextureFromGlobal(KernelMap[ComputeKernels.Draw], SolverId, SolverTexId);
            computeShader.Dispatch(
                KernelMap[ComputeKernels.Draw],
                Mathf.CeilToInt((float)SolverTex.width / GPUThreads.X),
                Mathf.CeilToInt((float)SolverTex.height / GPUThreads.Y),
                1
            );

            Shader.SetGlobalTexture(SolverTexId, SolverTex);
        }

        private void OnDestroy()
        {
            CleanUp();
        }

        #endregion

        #region Initialize

        protected virtual void Initialize()
        {
            InitialCheck();

            KernelMap = Enum.GetValues(typeof(ComputeKernels))
                .Cast<ComputeKernels>()
                .ToDictionary(t => t, t => computeShader.FindKernel(t.ToKernelName()));

            computeShader.GetKernelThreadGroupSizes(
                KernelMap[ComputeKernels.Draw],
                out var threadX,
                out var threadY,
                out var threadZ
            );

            GPUThreads = new GpuThreads(threadX, threadY, threadZ);
            SolverTexId = Shader.PropertyToID(solverTexProp);

            SolverId = Shader.PropertyToID(SolverProp);
            DensityId = Shader.PropertyToID(DensityProp);
            VelocityId = Shader.PropertyToID(VelocityProp);
            PrevId = Shader.PropertyToID(PrevProp);
            SourceId = Shader.PropertyToID(SourceProp);
            DiffId = Shader.PropertyToID(DiffProp);
            ViscId = Shader.PropertyToID(ViscProp);
            DTId = Shader.PropertyToID(DTProp);
            VelocityCoefId = Shader.PropertyToID(VelocityCoefProp);
            DensityCoefId = Shader.PropertyToID(DensityCoefProp);

            InitializeComputeShader();

            if (!debug) return;
            if (debugMat == null) return;
            debugMat.mainTexture = SolverTex;
        }

        protected virtual void InitialCheck()
        {
            Assert.IsTrue(SystemInfo.graphicsShaderLevel >= 50,
                "Under the DirectCompute5.0 (DX11 GPU) doesn't work : StableFluid");
            Assert.IsTrue(GPUThreads.X * GPUThreads.Y * GPUThreads.Z <= DirectCompute5_0.MaxProcess,
                "Resolution is too heigh : Stablefluid");
            Assert.IsTrue(GPUThreads.X <= DirectCompute5_0.MaxX, "THREAD_X is too large : StableFluid");
            Assert.IsTrue(GPUThreads.Y <= DirectCompute5_0.MaxY, "THREAD_Y is too large : StableFluid");
            Assert.IsTrue(GPUThreads.Z <= DirectCompute5_0.MaxZ, "THREAD_Z is too large : StableFluid");
        }

        protected abstract void InitializeComputeShader();

        #endregion

        #region UniLBM gpu kernel steps

        protected abstract void DensityStep();

        protected abstract void VelocityStep();

        #endregion

        #region render texture

        public RenderTexture CreateRenderTexture(int width, int height, int depth, RenderTextureFormat format,
            RenderTexture rt = null)
        {
            if (rt != null)
                if (rt.width == width && rt.height == height)
                    return rt;

            ReleaseRenderTexture(rt);
            rt = new RenderTexture(width, height, depth, format)
            {
                enableRandomWrite = true,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            rt.Create();
            ClearRenderTexture(rt, Color.clear);
            return rt;
        }

        protected static RenderTexture CreateVolumetricRenderTexture(int width, int height, int volumeDepth, int depth,
            RenderTextureFormat format, RenderTexture rt = null)
        {
            if (rt && rt.width == width && rt.height == height)
                return rt;

            ReleaseRenderTexture(rt);
            rt = new RenderTexture(width, height, depth, format)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = volumeDepth,
                enableRandomWrite = true,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            rt.Create();
            ClearRenderTexture(rt, Color.clear);
            return rt;
        }

        private static void ReleaseRenderTexture(RenderTexture rt)
        {
            if (!rt) return;

            rt.Release();
            Destroy(rt);
        }

        private static void ClearRenderTexture(RenderTexture target, Color bg)
        {
            var active = RenderTexture.active;
            RenderTexture.active = target;
            GL.Clear(true, true, bg);
            RenderTexture.active = active;
        }

        #endregion
    }
}