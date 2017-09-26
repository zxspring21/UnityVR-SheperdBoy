using System;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/VR Polish")]
public class VRPolish : PostProcessEffectBase 
{
	public enum DisplayMode
	{
		normal = 0,
		noFog = 1,
		debugDepthOfField = 2
	}
	public DisplayMode currentDisplayMode = DisplayMode.normal;

	// General variables
	public Shader effectShader;
	private Material effectMaterial;

	// Fog Variables
	public float fogHeight = 1.0f;
	public float fogStartDistance = 0.0f;

	[Range(0.001f, 10.0f)]
	public float fogDensity = 2.0f;

	// Depth of field Variables
	protected Vector4 depthFocus = new Vector4(0, .1f, .9f, 1f);
	protected Vector2 depthBlur = new Vector2(4.0f, 8.0f);

	protected bool sceneFogValuesUpdated = true;
	protected bool depthValuesUpdated = true;

	public override bool CheckResources()
	{
		CheckSupport(true);

		effectMaterial = CreateMaterial(effectShader, effectMaterial, true);

		if (!isSupported)
		{
			ReportAutoDisable();
		}
		return isSupported;
	}

	public void OnRenderImage(RenderTexture source, RenderTexture destination) 
	{
		// Figure out camera settings for calculating fog distances
		var CAMERA_NEAR = effectCamera.nearClipPlane;
		var CAMERA_FAR = effectCamera.farClipPlane;
		var CAMERA_FOV = effectCamera.fieldOfView;
		var CAMERA_ASPECT = effectCamera.aspect;
	
		var frustumCorners = Matrix4x4.identity;		
		var fovWHalf = CAMERA_FOV * 0.5f;
		var fovTan = Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

		var toRight = effectCameraTransform.right * CAMERA_NEAR * fovTan * CAMERA_ASPECT;
		var toTop = effectCameraTransform.up * CAMERA_NEAR * fovTan;
	
		var topLeft = (effectCameraTransform.forward * CAMERA_NEAR - toRight + toTop);
		var CAMERA_SCALE = topLeft.magnitude * (CAMERA_FAR/CAMERA_NEAR);
			
		topLeft.Normalize();
		topLeft *= CAMERA_SCALE;
	
		var topRight = (effectCameraTransform.forward * CAMERA_NEAR + toRight + toTop);
		topRight.Normalize();
		topRight *= CAMERA_SCALE;
		
		var bottomRight = (effectCameraTransform.forward * CAMERA_NEAR + toRight - toTop);
		bottomRight.Normalize();
		bottomRight *= CAMERA_SCALE;
		
		var bottomLeft = (effectCameraTransform.forward * CAMERA_NEAR - toRight - toTop);
		bottomLeft.Normalize();
		bottomLeft *= CAMERA_SCALE;
		
		frustumCorners.SetRow (0, topLeft); 
		frustumCorners.SetRow (1, topRight);		
		frustumCorners.SetRow (2, bottomRight);
		frustumCorners.SetRow (3, bottomLeft);		
		
		var camPos = effectCameraTransform.position;
		var FdotC = camPos.y - fogHeight;
		var paramK = (FdotC <= 0.0f ? 1.0f : 0.0f);
		effectMaterial.SetMatrix ("_FrustumCornersWS", frustumCorners);
		effectMaterial.SetVector ("_CameraWS", camPos);
		effectMaterial.SetVector ("_HeightParams", new Vector4(fogHeight, FdotC, paramK, fogDensity*0.5f));
		effectMaterial.SetVector ("_DistanceParams", new Vector4(-Mathf.Max(fogStartDistance,0.0f), 0, 0, 0));
		
		if (sceneFogValuesUpdated)
		{
			var sceneMode = RenderSettings.fogMode;
			var sceneDensity = RenderSettings.fogDensity;
			var sceneStart = RenderSettings.fogStartDistance;
			var sceneEnd = RenderSettings.fogEndDistance;
			var sceneParams = Vector4.zero;
			var linear = (sceneMode == UnityEngine.FogMode.Linear);
			var diff = linear ? (sceneEnd - sceneStart) : 0.0f;
			var invDiff = Mathf.Abs(diff) > 0.0001f ? 1.0f / diff : 0.0f;
			sceneParams.x = sceneDensity * 1.2011224087f; // density / sqrt(ln(2)), used by Exp2 fog mode
			sceneParams.y = sceneDensity * 1.4426950408f; // density / ln(2), used by Exp fog mode
			sceneParams.z = linear ? -invDiff : 0.0f;
			sceneParams.w = linear ? sceneEnd * invDiff : 0.0f;
			effectMaterial.SetVector ("_SceneFogParams", sceneParams);
			effectMaterial.SetInt ("_SceneFogMode", (int)sceneMode);
			sceneFogValuesUpdated = false;
		}
		if (depthValuesUpdated)
		{
			effectMaterial.SetVector("_DepthOfField", depthFocus);
			effectMaterial.SetVector("_BlurScale", depthBlur);
			depthValuesUpdated = false;
		}
		
		CustomGraphicsBlit (source, destination, effectMaterial, (int)currentDisplayMode);
	}

	public void SetDepthRaw(float minDistance, float focusStart, float focusEnd, float maxDistance)
	{
		//Debug.Log("Near: " + minDistance + ", fStart: " + focusStart + ", fEnd: " + focusEnd + ", maxDist: " + maxDistance);
		depthFocus.Set(minDistance, focusStart, focusEnd, maxDistance);
		depthValuesUpdated = true;
	}
	public void SetBlur(float nearBlur, float farBlur)
	{
		depthBlur.Set(nearBlur, farBlur);
		depthValuesUpdated = true;
	}

	static void CustomGraphicsBlit(RenderTexture source, RenderTexture destination, Material fxMaterial, int passNumber)
	{
		Graphics.SetRenderTarget(destination);
		
		fxMaterial.SetTexture("_MainTex", source);

		GL.PushMatrix ();
		GL.LoadOrtho ();	
	    	
		fxMaterial.SetPass (passNumber);	
		
	    GL.Begin (GL.QUADS);
							
		// Bottom Left
		GL.MultiTexCoord2 (0, 0.0f, 0.0f); 
		GL.Vertex3 (0.0f, 0.0f, 3.0f);
		
		// Bottom Right
		GL.MultiTexCoord2 (0, 1.0f, 0.0f); 
		GL.Vertex3 (1.0f, 0.0f, 2.0f);
		
		// Top Right
		GL.MultiTexCoord2 (0, 1.0f, 1.0f); 
		GL.Vertex3 (1.0f, 1.0f, 1.0f);
		
		// Top Left
		GL.MultiTexCoord2 (0, 0.0f, 1.0f); 
		GL.Vertex3 (0.0f, 1.0f, 0.0f);
		
		GL.End ();
	    GL.PopMatrix ();
	}
}
