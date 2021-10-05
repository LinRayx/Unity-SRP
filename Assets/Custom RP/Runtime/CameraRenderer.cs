using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{

	ScriptableRenderContext context;

	Camera camera;

	const string bufferName = "Render Camera";

	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};

	CullingResults cullingResults;

	static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
		litShaderTagId = new ShaderTagId("CustomLit");

	Lighting lighting = new Lighting();

	PostFXStack postFXStack = new PostFXStack();

	static int framebufferId = Shader.PropertyToID("_CameraFrameBuffer");

	public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, 
		bool useGPUInstancing, PostFXSettings postFXSettings)
	{
		this.context = context;
		this.camera = camera;

		PrepareBuffer();
		PrepareForSceneWindow();

		if (!Cull())
		{
			return;
		}
		buffer.BeginSample(SampleName);
		ExecuteBuffer();

		lighting.Setup(context, cullingResults);
		postFXStack.Setup(context, camera, postFXSettings);
		buffer.EndSample(SampleName);

		Setup();
		DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
		DrawUnsupportedShaders();
		DrawGizmosBeforeFX();
		if (postFXStack.IsActive)
        {
			postFXStack.Render(framebufferId);
        }
		DrawGizmosAfterFX();
		Cleanup();
		Submit();
	}

	void Submit()
    {
		buffer.EndSample(SampleName);
		ExecuteBuffer();
		context.Submit();
    }
	void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
	{
		var sortingSettings = new SortingSettings(camera)
		{
			criteria = SortingCriteria.CommonOpaque
		};
		var drawingSettings = new DrawingSettings(
			unlitShaderTagId, sortingSettings)
		{
			enableDynamicBatching = useDynamicBatching,
			enableInstancing = useGPUInstancing
		};
		drawingSettings.SetShaderPassName(1, litShaderTagId);
		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
		context.DrawSkybox(camera);

		sortingSettings.criteria = SortingCriteria.CommonTransparent;
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}

	void Setup()
	{
		context.SetupCameraProperties(camera);
		CameraClearFlags flags = camera.clearFlags;

		if (postFXStack.IsActive)
		{
			if (flags > CameraClearFlags.Color)
			{
				flags = CameraClearFlags.Color;
			}
			buffer.GetTemporaryRT(
				framebufferId, camera.pixelWidth, camera.pixelHeight,
				32, FilterMode.Bilinear, RenderTextureFormat.Default
			);
			buffer.SetRenderTarget(
				framebufferId,
				RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
			);
		}

		buffer.ClearRenderTarget(
			flags <= CameraClearFlags.Depth,
			flags == CameraClearFlags.Color,
			flags == CameraClearFlags.Color ?
				camera.backgroundColor.linear : Color.clear
		);
		buffer.BeginSample(SampleName);
		ExecuteBuffer();
	}

	void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	bool Cull()
	{
		//ScriptableCullingParameters p
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
		{
			cullingResults = context.Cull(ref p);

			return true;
		}
		return false;
	}

	void Cleanup()
    {
		lighting.Cleanup();
		if (postFXStack.IsActive)
        {
			buffer.ReleaseTemporaryRT(framebufferId);
		}
	}
}