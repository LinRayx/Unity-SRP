using UnityEngine;
using UnityEngine.Rendering;

public partial class PostFXStack
{
	enum Pass
	{
		BloomHorizontal,
		BloomVertical,
		Copy
	}
	const string bufferName = "Post FX";

	const int maxBloomPyramidLevels = 16;

	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};

	ScriptableRenderContext context;

	Camera camera;

	PostFXSettings settings;

	public bool IsActive => settings != null;

	int fxSourceId = Shader.PropertyToID("_PostFXSource");

	int bloomPyramidId;
	public PostFXStack()
	{
		bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
		for (int i = 1; i < maxBloomPyramidLevels * 2; i++)
		{
			Shader.PropertyToID("_BloomPyramid" + i);
		}
	}

	void DoBloom(int sourceId)
    {
		PostFXSettings.BloomSettings bloom = settings.Bloom;
		buffer.BeginSample("Bloom");
		int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
		RenderTextureFormat format = RenderTextureFormat.Default;
		int fromId = sourceId, toId = bloomPyramidId+1;
		int i;
		for (i = 0; i < bloom.maxIterations; i++)
		{
			if (height < bloom.downscaleLimit || width < bloom.downscaleLimit)
			{
				break;
			}
			int midId = toId - 1;
			buffer.GetTemporaryRT(
				midId, width, height, 0, FilterMode.Bilinear, format
			);
			buffer.GetTemporaryRT(
				toId, width, height, 0, FilterMode.Bilinear, format
			);
			Draw(fromId, midId, Pass.BloomHorizontal);
			Draw(midId, toId, Pass.BloomVertical);
			fromId = toId;
			toId += 2;
			width /= 2;
			height /= 2;
		}
		Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);

		for (i -= 1; i >= 0; i--)
		{
			buffer.ReleaseTemporaryRT(fromId);
			buffer.ReleaseTemporaryRT(fromId - 1);
			fromId -= 2;
		}
		buffer.EndSample("Bloom");
	}

	public void Setup(
		ScriptableRenderContext context, Camera camera, PostFXSettings settings
	)
	{
		this.context = context;
		this.camera = camera;

		this.settings =
			camera.cameraType <= CameraType.SceneView ? settings : null;
		ApplySceneViewState();
	}

	public void Render(int sourceId)
    {
		// Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
		DoBloom(sourceId);
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
	{
		buffer.SetGlobalTexture(fxSourceId, from);
		buffer.SetRenderTarget(
			to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
		);
		buffer.DrawProcedural(
			Matrix4x4.identity, settings.Material, (int)pass,
			MeshTopology.Triangles, 3
		);
	}
}