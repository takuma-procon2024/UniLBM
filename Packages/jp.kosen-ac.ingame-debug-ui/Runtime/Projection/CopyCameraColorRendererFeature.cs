using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Projection
{
    public class CopyCameraColorRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private RenderTexture cameraOutput;

        private CopyCameraColorRenderPass _copyCameraColorRenderPass;

        public override void Create()
        {
            _copyCameraColorRenderPass = new CopyCameraColorRenderPass(cameraOutput);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_copyCameraColorRenderPass);
        }
    }
}