using UnityEngine;

namespace Solver
{
    public class Solver3D : SolverBase
    {
        #region Variables

        [SerializeField] private int depth;

        #endregion

        #region Initialize

        protected override void InitializeComputeShader()
        {
            Width = Screen.width;
            Height = Screen.height;

            // 各種レンダーテクスチャを生成
            SolverTex = CreateVolumetricRenderTexture(Width >> lod, Height >> lod, depth >> lod, 0,
                RenderTextureFormat.ARGBFloat, SolverTex);
            DensityTex = CreateVolumetricRenderTexture(Width >> lod, Height >> lod, depth >> lod, 0,
                RenderTextureFormat.ARGBFloat, DensityTex);
            VelocityTex = CreateVolumetricRenderTexture(Width >> lod, Height >> lod, depth >> lod, 0,
                RenderTextureFormat.ARGBFloat, VelocityTex);
            PrevTex = CreateVolumetricRenderTexture(Width >> lod, Height >> lod, depth >> lod, 0,
                RenderTextureFormat.ARGBFloat, PrevTex);

            // 全てのシェーダーでSolverTexを参照できるようにする
            Shader.SetGlobalTexture(SolverTexId, SolverTex);

            // ComputeShaderに各種パラメータをセット
            computeShader.SetFloat(DiffId, diff);
            computeShader.SetFloat(ViscId, visc);
            computeShader.SetFloat(DTId, Time.deltaTime);
            computeShader.SetFloat(VelocityCoefId, velocityCoef);
            computeShader.SetFloat(DensityCoefId, densityCoef);
        }

        #endregion

        #region UniLBM gpu kernel steps

        protected override void DensityStep()
        {
            var dispatchX = Mathf.CeilToInt((float)SolverTex.width / GPUThreads.X);
            var dispatchY = Mathf.CeilToInt((float)SolverTex.height / GPUThreads.Y);
            var dispatchZ = Mathf.CeilToInt((float)depth / GPUThreads.Z);

            // Add density source to density field
            if (SourceTex)
            {
                computeShader.SetTexture(KernelMap[ComputeKernels.AddSourceDensity], SourceId, SourceTex);
                computeShader.SetTexture(KernelMap[ComputeKernels.AddSourceDensity], DensityId, DensityTex);
                computeShader.SetTexture(KernelMap[ComputeKernels.AddSourceDensity], PrevId, PrevTex);
                computeShader.Dispatch(KernelMap[ComputeKernels.AddSourceDensity], dispatchX, dispatchY, dispatchZ);
            }

            // Diffuse density
            computeShader.SetTexture(KernelMap[ComputeKernels.DiffuseDensity], DensityId, DensityTex);
            computeShader.SetTexture(KernelMap[ComputeKernels.DiffuseDensity], PrevId, PrevTex);
            computeShader.Dispatch(KernelMap[ComputeKernels.DiffuseDensity], dispatchX, dispatchY, dispatchZ);

            // Swap density
            computeShader.SetTexture(KernelMap[ComputeKernels.SwapDensity], DensityId, DensityTex);
            computeShader.SetTexture(KernelMap[ComputeKernels.SwapDensity], PrevId, PrevTex);
            computeShader.Dispatch(KernelMap[ComputeKernels.SwapDensity], dispatchX, dispatchY, dispatchZ);

            if (isDensityOnly)
            {
                // Advection using external velocity field via ForceTex
                computeShader.SetTexture(KernelMap[ComputeKernels.AdvectDensityFromExt], DensityId, DensityTex);
                computeShader.SetTexture(KernelMap[ComputeKernels.AdvectDensityFromExt], PrevId, PrevTex);
                computeShader.SetTexture(KernelMap[ComputeKernels.AdvectDensityFromExt], VelocityId, VelocityTex);
                if (SourceTex)
                    computeShader.SetTexture(KernelMap[ComputeKernels.AdvectDensityFromExt], SourceId, SourceTex);
                computeShader.Dispatch(KernelMap[ComputeKernels.AdvectDensityFromExt], dispatchX, dispatchY, dispatchZ);
            }
            else
            {
                // Advection using velocity solver               
                computeShader.SetTexture(KernelMap[ComputeKernels.AdvectDensity], DensityId, DensityTex);
                computeShader.SetTexture(KernelMap[ComputeKernels.AdvectDensity], PrevId, PrevTex);
                computeShader.SetTexture(KernelMap[ComputeKernels.AdvectDensity], VelocityId, VelocityTex);
                computeShader.Dispatch(KernelMap[ComputeKernels.AdvectDensity], dispatchX, dispatchY, dispatchZ);
            }
        }

        protected override void VelocityStep()
        {
            var dispatchX = Mathf.CeilToInt((float)SolverTex.width / GPUThreads.X);
            var dispatchY = Mathf.CeilToInt((float)SolverTex.height / GPUThreads.Y);
            var dispatchZ = Mathf.CeilToInt((float)depth / GPUThreads.Z);

            // Add velocity source to velocity field
            if (SourceTex)
            {
                computeShader.SetTexture(KernelMap[ComputeKernels.AddSourceVelocity], SourceId, SourceTex);
                computeShader.SetTexture(KernelMap[ComputeKernels.AddSourceVelocity], VelocityId, VelocityTex);
                computeShader.SetTexture(KernelMap[ComputeKernels.AddSourceVelocity], PrevId, PrevTex);
                computeShader.Dispatch(KernelMap[ComputeKernels.AddSourceVelocity], dispatchX, dispatchY, dispatchZ);
            }

            // Diffuse velocity
            computeShader.SetTexture(KernelMap[ComputeKernels.DiffuseVelocity], VelocityId, VelocityTex);
            computeShader.SetTexture(KernelMap[ComputeKernels.DiffuseVelocity], PrevId, PrevTex);
            computeShader.Dispatch(KernelMap[ComputeKernels.DiffuseVelocity], dispatchX, dispatchY, dispatchZ);

            // Project
            Project();

            // Swap velocity
            computeShader.SetTexture(KernelMap[ComputeKernels.SwapVelocity], VelocityId, VelocityTex);
            computeShader.SetTexture(KernelMap[ComputeKernels.SwapVelocity], PrevId, PrevTex);
            computeShader.Dispatch(KernelMap[ComputeKernels.SwapVelocity], dispatchX, dispatchY, dispatchZ);

            // Advection
            computeShader.SetTexture(KernelMap[ComputeKernels.AdvectVelocity], DensityId, DensityTex);
            computeShader.SetTexture(KernelMap[ComputeKernels.AdvectVelocity], VelocityId, VelocityTex);
            computeShader.SetTexture(KernelMap[ComputeKernels.AdvectVelocity], PrevId, PrevTex);
            computeShader.Dispatch(KernelMap[ComputeKernels.AdvectVelocity], dispatchX, dispatchY, dispatchZ);

            // Project
            Project();
            return;

            void Project()
            {
                // Project
                computeShader.SetTexture(KernelMap[ComputeKernels.ProjectStep1], VelocityId, VelocityTex);
                computeShader.SetTexture(KernelMap[ComputeKernels.ProjectStep1], PrevId, PrevTex);
                computeShader.Dispatch(KernelMap[ComputeKernels.ProjectStep1], dispatchX, dispatchY, dispatchZ);

                // Project
                computeShader.SetTexture(KernelMap[ComputeKernels.ProjectStep2], VelocityId, VelocityTex);
                computeShader.SetTexture(KernelMap[ComputeKernels.ProjectStep2], PrevId, PrevTex);
                computeShader.Dispatch(KernelMap[ComputeKernels.ProjectStep2], dispatchX, dispatchY, dispatchZ);

                // Project
                computeShader.SetTexture(KernelMap[ComputeKernels.ProjectStep3], VelocityId, VelocityTex);
                computeShader.SetTexture(KernelMap[ComputeKernels.ProjectStep3], PrevId, PrevTex);
                computeShader.Dispatch(KernelMap[ComputeKernels.ProjectStep3], dispatchX, dispatchY, dispatchZ);
            }
        }

        #endregion
    }
}