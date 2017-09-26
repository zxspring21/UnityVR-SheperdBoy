Shader "Hidden/VRPolish" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "black" {}
}

CGINCLUDE

	#include "UnityCG.cginc"

	uniform sampler2D _MainTex;
	uniform sampler2D_float _CameraDepthTexture;
	
	// x = fog height
	// y = FdotC (CameraY-FogHeight)
	// z = k (FdotC > 0.0)
	// w = a/2
	uniform float4 _HeightParams;
	
	// x = start distance
	uniform float4 _DistanceParams;
	
	int _SceneFogMode;
	float4 _SceneFogParams;
	#ifndef UNITY_APPLY_FOG
	half4 unity_FogColor;
	half4 unity_FogDensity;
	#endif	

	uniform float4 _MainTex_TexelSize;
	
	// for fast world space reconstruction
	uniform float4x4 _FrustumCornersWS;
	uniform float4 _CameraWS;

	// Depth of field variables
	uniform float4 _DepthOfField;
	uniform float2 _BlurScale;

	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		float2 uv_depth : TEXCOORD1;
		float4 interpolatedRay : TEXCOORD2;
	};
	
	v2f vert (appdata_img v)
	{
		v2f o;
		half index = v.vertex.z;
		v.vertex.z = 0.1;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv = v.texcoord.xy;
		o.uv_depth = v.texcoord.xy;
		
		#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0)
			o.uv.y = 1-o.uv.y;
		#endif				
		
		o.interpolatedRay = _FrustumCornersWS[(int)index];
		o.interpolatedRay.w = index;
		
		return o;
	}
	
	// Applies one of standard fog formulas, given fog coordinate (i.e. distance)
	half ComputeFogFactor (float coord)
	{
		float fogFac = 0.0;
		if (_SceneFogMode == 1) // linear
		{
			// factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
			fogFac = coord * _SceneFogParams.z + _SceneFogParams.w;
		}
		if (_SceneFogMode == 2) // exp
		{
			// factor = exp(-density*z)
			fogFac = _SceneFogParams.y * coord; fogFac = exp2(-fogFac);
		}
		if (_SceneFogMode == 3) // exp2
		{
			// factor = exp(-(density*z)^2)
			fogFac = _SceneFogParams.x * coord; fogFac = exp2(-fogFac*fogFac);
		}
		return saturate(fogFac);
	}

	// Distance-based fog
	float ComputeDistance (float3 camDir)
	{
		float coord = length(camDir);
		return coord;
	}

	// Linear half-space fog, from https://www.terathon.com/lengyel/Lengyel-UnifiedFog.pdf
	float ComputeHalfSpace (float3 wsDir)
	{
		float3 wpos = _CameraWS + wsDir;
		float FH = _HeightParams.x;
		float3 C = _CameraWS;
		float3 V = wsDir;
		float3 P = wpos;
		float3 aV = _HeightParams.w * V *.001;
		float FdotC = _HeightParams.y;
		float k = _HeightParams.z;
		float FdotP = P.y-FH;
		float FdotV = wsDir.y;
		float c1 = k * (FdotP + FdotC);
		float c2 = (1-2*k) * FdotP;
		float g = min(c2, 0.0);
		g = -length(aV) * (c1 - g * g / abs(FdotV+1.0e-5f));
		return g;
	}

	float computeDepthFactor(half minDistance, half maxDistance, half currentValue)
	{
		return saturate((currentValue - minDistance) / (maxDistance - minDistance));
	}

	half4 DoDepthOfField(v2f i, float depth) : SV_Target
	{
		// First with the depth calculate the depth of field
		float nearDepth = saturate((depth - _DepthOfField.x) / (_DepthOfField.y - _DepthOfField.x));
		float farDepth = saturate((_DepthOfField.w - depth) / (_DepthOfField.w - _DepthOfField.z));

		float texelFactor = max(_BlurScale.x * (1 - nearDepth), _BlurScale.y * (1 - farDepth));

		half4 sceneColor = (tex2D(_MainTex, i.uv) +
							tex2D(_MainTex, i.uv + fixed2(_MainTex_TexelSize.x*texelFactor, 0)) +
							tex2D(_MainTex, i.uv - fixed2(_MainTex_TexelSize.x*texelFactor, 0)) +
							tex2D(_MainTex, i.uv + fixed2(0, _MainTex_TexelSize.y*texelFactor)) +
							tex2D(_MainTex, i.uv - fixed2(0, _MainTex_TexelSize.y*texelFactor)))*.2;
		return sceneColor;
	}

	half4 DoAll(v2f i) : SV_Target
	{
		// First with the depth calculate the depth of field
		float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,i.uv_depth));
		half4 sceneColor = DoDepthOfField(i, depth);

		// Now handle fog
		// Reconstruct world space position & direction
		// towards this screen pixel.
		float4 wsDir = depth * i.interpolatedRay;
		float4 wsPos = _CameraWS + wsDir;

		// Compute fog distance
		float g = _DistanceParams.x + ComputeDistance(wsDir);

		// Compute fog amount
		half fogFac = max(ComputeFogFactor (max(0.0,g)), 1 - saturate(ComputeHalfSpace(wsDir)));

		//return fogFac; // for debugging
		
		// Lerp between fog color & original scene color
		// by fog amount
		return lerp (unity_FogColor, sceneColor, fogFac);
	}

	half4 DisplayDepthOfFieldRange(v2f i) : SV_TARGET
	{
		float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,i.uv_depth));
		float segment1 = 1 - step(_DepthOfField.x,depth);
		float segment2 = (1 - step(_DepthOfField.y, depth)) * step(_DepthOfField.x,depth);
		float segment3 = (1 - step(_DepthOfField.z, depth)) * step(_DepthOfField.y, depth);
		float segment4 = (1 - step(_DepthOfField.w, depth)) * step(_DepthOfField.z, depth);

		float nearDepth = saturate((depth - _DepthOfField.x) / (_DepthOfField.y - _DepthOfField.x));
		float farDepth = saturate((_DepthOfField.w - depth) / (_DepthOfField.w - _DepthOfField.z));

		float texelFactor = max(_BlurScale.x * (1 - nearDepth), _BlurScale.y * (1 - farDepth));

		half4 sceneColor = (tex2D(_MainTex, i.uv + fixed2(_MainTex_TexelSize.x*texelFactor, 0)) +
			tex2D(_MainTex, i.uv - fixed2(_MainTex_TexelSize.x*texelFactor, 0)) +
			tex2D(_MainTex, i.uv + fixed2(0, _MainTex_TexelSize.y*texelFactor)) +
			tex2D(_MainTex, i.uv - fixed2(0, _MainTex_TexelSize.y*texelFactor)))*.25;

		return saturate((half4(1, 0, 0, 1)*segment1) + (half4(0,1,0,1)*segment2) + (half4(0,0,1,1)*segment3) + (half4(1, 1, 1, 1)*segment4))*.5 + sceneColor*.5;
	}
ENDCG

SubShader
{
	ZTest Always Cull Off ZWrite Off Fog { Mode Off }

	// 0: Normal
	Pass
	{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		half4 frag (v2f i) : SV_Target { return DoAll (i); }
		ENDCG
	}
	// 1: No Fog
	Pass
	{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		half4 frag(v2f i) : SV_Target{ return DoDepthOfField(i, Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,i.uv_depth))); }
		ENDCG
	}
	// 2: Fog Range Debug
	Pass
	{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		half4 frag(v2f i) : SV_Target{ return DisplayDepthOfFieldRange(i); }
		ENDCG
	}
}

Fallback off

}
