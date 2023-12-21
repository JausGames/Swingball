using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ShockwaveRenderFeature : ScriptableRendererFeature
{
    public Material shockwaveMaterial;

    private ShockwaveRenderPass shockwavePass;

    public override void Create()
    {
        shockwavePass = new ShockwaveRenderPass(shockwaveMaterial);
        // Configure the pass here if needed
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        shockwavePass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(shockwavePass);
    }
}
