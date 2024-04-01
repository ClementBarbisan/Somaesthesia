using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class ColorBlitRendererFeature : ScriptableRendererFeature
{
    public Material m_Material;

    ColorBlitPass m_RenderPass = null;

    public override void AddRenderPasses(ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (renderingData.cameraData.camera == Camera.main)
            renderer.EnqueuePass(m_RenderPass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer,
        in RenderingData renderingData)
    {
        if (renderingData.cameraData.camera == Camera.main)
        {
            // Calling ConfigureInput with the ScriptableRenderPassInput.Color argument
            // ensures that the opaque texture is available to the Render Pass.
            m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
            m_RenderPass.SetTarget(renderer.cameraColorTargetHandle);
        }
    }

    public override void Create()
    {
        m_RenderPass = new ColorBlitPass(m_Material, FindObjectOfType<SendSkeletonToShader>(true));
    }

    protected override void Dispose(bool disposing)
    {
    }
}

internal class ColorBlitPass : ScriptableRenderPass
{
    ProfilingSampler m_ProfilingSampler = new ProfilingSampler("ColorBlit");
    Material m_Material;
    RTHandle m_CameraColorTarget;
    private Camera _mainCamera;
    private SendSkeletonToShader _skeleton;
    public ColorBlitPass(Material material, SendSkeletonToShader skeleton)
    {
        m_Material = material;
        _skeleton = skeleton;
        renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
    }

    public void SetTarget(RTHandle colorHandle)
    {
        m_CameraColorTarget = colorHandle;
        _mainCamera = Camera.main;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        ConfigureTarget(m_CameraColorTarget);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;
        if (cameraData.camera != _mainCamera)
            return;

        if (m_Material == null)
            return;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            m_Material.SetFloat("_Value", _skeleton.sizeSkeleton / _skeleton.maxSkeleton);
            Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, m_CameraColorTarget, m_Material, 0);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }
}