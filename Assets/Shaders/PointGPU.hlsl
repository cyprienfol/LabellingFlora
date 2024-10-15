#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	StructuredBuffer<float3> _Positions;
	StructuredBuffer<float3> _Colors;
#endif

float _PointSize;
float _Scale;
float _Theta;
float3 _Anchor;
float4 _Axis;
//float4 _brushTipPosiion;

float3 Rotate(float3 position)
{
	float3 axis = float3(_Axis.x, _Axis.y, _Axis.z);
	if (axis.x + axis.y + axis.z == 0)
		axis = float3(1.0, 1.0, 1.0);
	axis = normalize(axis);

	return float3((cos(_Theta) + axis.x * axis.x * (1 - cos(_Theta))) * position.x + (axis.x * axis.y * (1 - cos(_Theta)) - axis.z * sin(_Theta)) * position.y + (axis.x * axis.z * (1 - cos(_Theta)) + axis.y * sin(_Theta)) * position.z,
		(axis.y*axis.x*(1-cos(_Theta))+axis.z*sin(_Theta)) * position.x + (cos(_Theta)+axis.y*axis.y*(1-cos(_Theta))) * position.y + (axis.y*axis.z*(1-cos(_Theta))-axis.x*sin(_Theta)) * position.z,
		(axis.z*axis.x*(1-cos(_Theta))-axis.y*sin(_Theta)) * position.x + (axis.z*axis.y*(1-cos(_Theta))+axis.x*sin(_Theta)) * position.y + (cos(_Theta)+axis.z*axis.z*(1-cos(_Theta))) * position.z);
}

float3 Scale(float3 position)
{
	return (position - _Anchor) * _Scale + _Anchor ;
}

void ConfigureProcedural () {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		float3 position = _Positions[unity_InstanceID];
		_Axis = normalize(_Axis);
		unity_ObjectToWorld = 0.0;
		float3 transform = Scale(position);
		transform = Rotate(transform);

		// Vector column that corresponds to last column of the 4x4 transformation matrix.
		unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);
		//unity_ObjectToWorld._m03_m13_m23_m33 = float4(transform, 1.0);

		// Scale point size
		unity_ObjectToWorld._m00_m11_m22 = _PointSize;

	#endif
}



void ShaderGraphFunction_float (float3 In, out float3 Out) {
	Out = In;
}

void ShaderGraphFunction_half (half3 In, out half3 Out) {
	Out = In;
}

void ColorFunction_float(float3 In, out float3 Out) {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		Out = _Colors[unity_InstanceID];
	#else
		Out = In;
	#endif
}