using UnityEngine;
using System.Collections;

[RequireComponent(typeof(VRPolish))]
[AddComponentMenu("Image Effects/Depth of Field Controls")]
public class DepthOfFieldControls : MonoBehaviour 
{
	public float nearBlurFactor = 4f;
	public float farBlurFactor = 8f;

	[Tooltip("Any pixel closer than this depth will be fully blurred")]
	public float nearDepth = 10f;

	[Tooltip("This is the starting depth that a pixel will be fully in focus")]
	public float focusStart = 100f;

	[Tooltip("This is the ending depth where a pixel will be fully in focus.")]
	public float focusEnd = -100f;

	[Tooltip("Any pixel further than this depth will be fully blurred.")]
	public float maxDepth = 0f;

	[Tooltip("If true, focusEnd and maxDepth are relative to the camera far plane rather than absolute.")]
	public bool maxDepthRelativeToFarPlane = true;

	[Tooltip("If set, then the depth of field will focus around the given transform.")]
	public Transform dynamicFocusPoint = null;
	public float dynamicFocusRange = .1f;

	// Cached values
	protected VRPolish polishScript;
	protected Camera polishCamera;
	protected Transform camTransform;

	// Use this for initialization
	void Start () 
	{
		polishScript = GetComponent<VRPolish>();
		polishCamera = GetComponent<Camera>();
		camTransform = polishCamera.transform;
		UpdateDepthValues();
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (dynamicFocusPoint != null)
		{
			UpdateDynamicDepth();
		}
		else
		{
			UpdateDepthValues();
		}
	}

	void UpdateDynamicDepth()
	{
		// Get distance from focus point to camera
		var distToCamera = (camTransform.position - dynamicFocusPoint.position).magnitude;
		var farPlane = polishCamera.farClipPlane;

		// Use this to calculate focus values
		var dynamicNearDepth = (distToCamera - (distToCamera * dynamicFocusRange * 2)) / farPlane;
		var dynamicFocusStart = (distToCamera - (distToCamera * dynamicFocusRange)) / farPlane;

		var dynamicFocusEnd = (distToCamera + (distToCamera * dynamicFocusRange)) / farPlane;
		var dynamicFarDepth = (distToCamera + (distToCamera * dynamicFocusRange * 2)) / farPlane;

		// Make sure they don't go outside the static ranges we set initially
		var offset = Mathf.Max((focusStart/farPlane) - dynamicFocusStart, 0);
		if (maxDepthRelativeToFarPlane)
		{
			offset -= Mathf.Max(dynamicFocusEnd - ((focusEnd + farPlane) /farPlane), 0);
		}
		else
		{
			offset -= Mathf.Max(dynamicFocusEnd - (focusEnd/farPlane), 0);
		}
		dynamicNearDepth += offset;
		dynamicFocusStart += offset;
		dynamicFocusEnd += offset;
		dynamicFarDepth += offset;

		polishScript.SetDepthRaw(dynamicNearDepth, dynamicFocusStart, dynamicFocusEnd, dynamicFarDepth);
		polishScript.SetBlur(nearBlurFactor, farBlurFactor);
	}

	void UpdateDepthValues()
	{
		var farPlane = polishCamera.farClipPlane;
		if (maxDepthRelativeToFarPlane)
		{
			polishScript.SetDepthRaw(nearDepth / farPlane, focusStart / farPlane, (farPlane + focusEnd) / farPlane, (farPlane + maxDepth) / farPlane);
		}
		else
		{
			polishScript.SetDepthRaw(nearDepth / farPlane, focusStart / farPlane, focusEnd / farPlane, maxDepth / farPlane);
		}
		polishScript.SetBlur(nearBlurFactor, farBlurFactor);
	}
}
