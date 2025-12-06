using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class OutlineMaskFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Tooltip("Material that uses the Custom/OutlineMask shader.")]
        public Material maskMaterial;

        [Tooltip("When in the frame to render the outline mask.")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        [Tooltip("Which GameObject layers are allowed into the mask.")]
        public LayerMask layerMask = ~0;

        [Tooltip("Which Rendering Layer Mask (MeshRenderer 'Rendering Layer Mask') is required.")]
        public RenderingLayerMask renderingLayerMask =
            RenderingLayerMask.defaultRenderingLayerMask;

        [Tooltip("Also run in Scene view?")]
        public bool renderInSceneView = true;
    }

    public Settings settings = new Settings();

    // =====================================================================
    // RenderGraph PASS
    // =====================================================================

    class OutlineMaskPass : ScriptableRenderPass
    {
        const string k_ProfilerTag = "Outline Mask Pass (RenderGraph)";
        static readonly int _OutlineMaskTexID = Shader.PropertyToID("_OutlineMaskTex");

        readonly Settings _settings;

        class PassData
        {
            public RendererListHandle rendererList;
        }

        public OutlineMaskPass(Settings settings, string featureName)
        {
            _settings = settings;
            renderPassEvent = settings.renderPassEvent;
            profilingSampler = new ProfilingSampler(featureName);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            using IRasterRenderGraphBuilder builder =
                renderGraph.AddRasterRenderPass<PassData>(k_ProfilerTag, out PassData passData, profilingSampler);

            // Build renderer list
            InitPassData(renderGraph, frameData, ref passData);

            if (!passData.rendererList.IsValid())
                return;

            // Create the mask texture (R8, no depth)
            TextureHandle maskTexture = CreateMaskTexture(renderGraph, frameData);

            // Use camera depth as depth attachment
            var resourceData = frameData.Get<UniversalResourceData>();

            builder.UseRendererList(passData.rendererList);
            builder.SetRenderAttachment(maskTexture, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);

            // Make the texture visible as global _OutlineMaskTex
            builder.SetGlobalTextureAfterPass(maskTexture, _OutlineMaskTexID);

            builder.AllowGlobalStateModification(true);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
            {
                // clear color only, keep depth
                ctx.cmd.ClearRenderTarget(RTClearFlags.Color, Color.black, 0, 0);
                ctx.cmd.DrawRendererList(data.rendererList);
            });
        }

        // Build RendererList for the objects we want in the mask
        void InitPassData(RenderGraph renderGraph, ContextContainer frameData, ref PassData passData)
        {
            var urpData = frameData.Get<UniversalRenderingData>();
            var camera = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();

            var shaderTags = new List<ShaderTagId>
    {
        new ShaderTagId("UniversalForward"),
        new ShaderTagId("UniversalForwardOnly"),
        new ShaderTagId("SRPDefaultUnlit")
    };

            var drawingSettings = RenderingUtils.CreateDrawingSettings(
                shaderTags,
                urpData,
                camera,
                lightData,
                camera.defaultOpaqueSortFlags
            );

            drawingSettings.overrideMaterial = _settings.maskMaterial;

            // Only render objects that:
            //   - are on the selected GameObject layers (layerMask)
            //   - AND have the selected Rendering Layer bit(s) set
            var filtering = new FilteringSettings(RenderQueueRange.all, _settings.layerMask);
            filtering.renderingLayerMask = _settings.renderingLayerMask.value;

            var rlParams = new RendererListParams(urpData.cullResults, drawingSettings, filtering);
            passData.rendererList = renderGraph.CreateRendererList(rlParams);
        }

        // Create an R8 texture matching the camera size for the mask
        TextureHandle CreateMaskTexture(RenderGraph renderGraph, ContextContainer frameData)
        {
            var camera = frameData.Get<UniversalCameraData>();

            var desc = camera.cameraTargetDescriptor;
            desc.colorFormat = RenderTextureFormat.R8;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            return UniversalRenderer.CreateRenderGraphTexture(
                renderGraph,
                desc,
                "_OutlineMaskTex",
                false
            );
        }
    }

    OutlineMaskPass _pass;

    // =====================================================================
    // RendererFeature hooks
    // =====================================================================

    public override void Create()
    {
        if (settings.maskMaterial == null)
        {
            Debug.LogWarning("OutlineMaskFeature: Mask material is not assigned.");
            return;
        }

        _pass = new OutlineMaskPass(settings, name);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_pass == null)
            return;

        var camType = renderingData.cameraData.cameraType;

        if (camType == CameraType.Preview)
            return;

        if (!settings.renderInSceneView && camType == CameraType.SceneView)
            return;

        _pass.renderPassEvent = settings.renderPassEvent;
        renderer.EnqueuePass(_pass);
    }
}
