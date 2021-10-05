using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;

public class Lighting
{

	const string bufferName = "Lighting";

	const int maxDirLightCount = 4;
	const int maxSpotLightCount = 4;

	static int
		//dirLightColorId = Shader.PropertyToID("_DirectionalLightColor"),
		//dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");
		dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
		dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
		dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");

	static int
		spotLightCountsId = Shader.PropertyToID("_SpotLightCount"),
		spotLightColorsId = Shader.PropertyToID("_SpotLightColors"),
		spotLightPositionsId = Shader.PropertyToID("_SpotLightPositions"),
		spotLightDirectionsId = Shader.PropertyToID("_SpotLightDirections"),
		spotLightAnglesId = Shader.PropertyToID("_SpotLightAngles");

	static Vector4[]
		dirLightColors = new Vector4[maxDirLightCount],
		dirLightDirections = new Vector4[maxDirLightCount];

	static Vector4[]
		spotLightColors = new Vector4[maxSpotLightCount],
		spotLightPositions = new Vector4[maxSpotLightCount],
		spotLightDirections = new Vector4[maxSpotLightCount],
		spotLightAngles = new Vector4[maxSpotLightCount];

		CullingResults cullingResults;

	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};

	public void Setup(ScriptableRenderContext context, CullingResults cullingResults)
	{
		this.cullingResults = cullingResults;
		buffer.BeginSample(bufferName);
		SetupLights();
		buffer.EndSample(bufferName);
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	void SetupLights() {
		NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
		int dirLightCount = 0;
		int spotLightCount = 0;
		for (int i = 0; i < visibleLights.Length; i++)
		{
			VisibleLight visibleLight = visibleLights[i];

			switch (visibleLight.lightType)
            {
				case LightType.Directional:
                    {
						if(dirLightCount < maxDirLightCount)
						{
							SetupDirectionalLight(dirLightCount++, ref visibleLight);
						}
					}
					break;
				case LightType.Spot:
					if (spotLightCount < maxSpotLightCount)
					{
						SetupSpotLight(spotLightCount++, ref visibleLight);
					}
					break;
			}
		}

		buffer.SetGlobalInt(dirLightCountId, dirLightCount);
		buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
		buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);

		buffer.SetGlobalInt(spotLightCountsId, spotLightCount);
		buffer.SetGlobalVectorArray(spotLightPositionsId, spotLightPositions); ;
		buffer.SetGlobalVectorArray(spotLightDirectionsId, spotLightDirections);
		buffer.SetGlobalVectorArray(spotLightColorsId, spotLightColors);
		buffer.SetGlobalVectorArray(spotLightAnglesId, spotLightAngles);

	}
	void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
	{
		dirLightColors[index] = visibleLight.finalColor;
		dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
	}

	void SetupSpotLight(int index, ref VisibleLight visibleLight)
    {
		spotLightColors[index] = visibleLight.finalColor;
		spotLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
		Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
		position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
		spotLightPositions[index] = position;

		Light light = visibleLight.light;
		float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
		float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
		float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
		spotLightAngles[index] = new Vector4(
			angleRangeInv, -outerCos * angleRangeInv
		);
	}

	public void Cleanup()
    {

    }
}