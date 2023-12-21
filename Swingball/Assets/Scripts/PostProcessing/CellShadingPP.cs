using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[Serializable, VolumeComponentMenu("Post-processing/Custom/CellShadingPP")]
public sealed class CellShadingPP : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [SerializeField] private FloatParameter _posterizeAmount = new FloatParameter(1f);
    [SerializeField] private BoolParameter _isEnabled = new BoolParameter(false);
    [SerializeField] private FloatParameter _shadowValue = new FloatParameter(.1f);
    [SerializeField] private FloatParameter _shadowThreshold = new FloatParameter(0.05f);
    [SerializeField] private ColorParameter _shadowColor = new ColorParameter(Color.black);

    Material m_Material;

    public bool IsActive() => m_Material != null && _isEnabled.value;

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterOpaqueAndSky;

    const string kShaderName = "Hidden/Shader/CellShadingPP";

    public override void Setup()
    {
        if (Shader.Find(kShaderName) != null)
            m_Material = new Material(Shader.Find(kShaderName));
        else
            Debug.LogError(
                $"Unable to find shader '{kShaderName}'. Post Process Volume CellShadingPP is unable to load.");
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
            return;

        m_Material.SetFloat("_PosterizeAmount", _posterizeAmount.value);
        m_Material.SetFloat("_ShadowValue", _shadowValue.value);
        m_Material.SetFloat("_ShadowThreshold", _shadowThreshold.value);
        m_Material.SetColor("_ShadowColor", _shadowColor.value);

        m_Material.SetTexture("_InputTexture", source);
        HDUtils.DrawFullScreen(cmd, m_Material, destination);
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
}