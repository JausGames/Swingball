using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ShockwaveRenderPass : ScriptableRenderPass
{
    private Material shockwaveMaterial;
    private RenderTargetIdentifier currentTarget;

    public ShockwaveRenderPass(Material material)
    {
        shockwaveMaterial = material;
    }

    public void Setup(RenderTargetIdentifier target)
    {
        currentTarget = target;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // You can add custom logic here to draw with your material

        CommandBuffer cmd = CommandBufferPool.Get("ShockwaveEffect");

        cmd.Blit(currentTarget, currentTarget, shockwaveMaterial);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
