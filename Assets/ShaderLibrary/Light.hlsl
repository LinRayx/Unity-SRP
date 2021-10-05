#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

struct Light {
	float3 color;
	float3 direction;
	float attenuation;
};

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_SPOT_LIGHT_COUNT 4

CBUFFER_START(_CustomDirectionLight)
	//float4 _DirectionalLightColor;
	//float4 _DirectionalLightDirection;
	int _DirectionalLightCount;
	float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

CBUFFER_START(_CustomSpotLight)
	int _SpotLightCount;
	float4 _SpotLightColors[MAX_SPOT_LIGHT_COUNT];
	float4 _SpotLightDirections[MAX_SPOT_LIGHT_COUNT];
	float4 _SpotLightPositions[MAX_SPOT_LIGHT_COUNT]; 
	float4 _SpotLightAngles[MAX_SPOT_LIGHT_COUNT];
CBUFFER_END

int GetDirectionalLightCount () {
	return _DirectionalLightCount;
}

int GetSpotLightCount() {
	return _SpotLightCount;
}

Light GetDirectionalLight (int index) {
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	light.attenuation = 1.0f;
	return light;
}

Light GetSpotLight(int index, Surface surfaceWS) {
	Light light;
	light.color = _SpotLightColors[index].rgb;
	float3 ray = _SpotLightPositions[index].xyz - surfaceWS.position;
	light.direction = normalize(ray);
	float distanceSqr = max(dot(ray, ray), 0.00001);
	float rangeAttenuation = Square(
		saturate(1.0 - Square(distanceSqr * _SpotLightPositions[index].w))
	);
	float4 spotAngle = _SpotLightAngles[index];
	float spotAttenuation = Square(saturate(dot(_SpotLightDirections[index].xyz, light.direction) * spotAngle.x + spotAngle.y));
	light.attenuation = spotAttenuation * rangeAttenuation / distanceSqr;
	return light;
}
#endif