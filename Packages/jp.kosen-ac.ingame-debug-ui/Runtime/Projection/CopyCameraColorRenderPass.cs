using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Projection
{
    public class CopyCameraColorRenderPass : ScriptableRenderPass
    {
        private const string ProfilerTag = "Copy CameraColor Render Pass";

        private readonly RenderTexture _cameraOutput;
        private readonly ProfilingSampler _profilingSampler = new(ProfilerTag);

        public CopyCameraColorRenderPass(RenderTexture cameraOutput)
        {
            _cameraOutput = cameraOutput;
            renderPassEvent = RenderPassEvent.AfterRendering;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.isSceneViewCamera
                || renderingData.cameraData.isPreviewCamera
               ) return;

            ref var cameraData = ref renderingData.cameraData;
            var cmd = CommandBufferPool.Get(ProfilerTag);

            using (new ProfilingScope(cmd, _profilingSampler))
            {
                CoreUtils.SetRenderTarget(cmd, _cameraOutput);
                Blitter.BlitTexture(
                    cmd, cameraData.renderer.cameraColorTargetHandle,
                    new Vector4(1, 1, 0, 0), 0.0f, false
                );
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}