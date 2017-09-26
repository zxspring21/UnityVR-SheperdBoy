using UnityEngine;
using UnityEngine.VR;

[AddComponentMenu("Shepherd/Mode Control")]
public class ModeControl : MonoBehaviour 
{
	public GameObject ARCamera;
	public GameObject DesktopCamera;
	public GameObject VRCamera;

	protected CameraPositionControl cameraControls;

	public enum programMode
	{
		autodetect = 0,
		desktop = 1,
		hololens = 2,
		unityVR = 3
	}
	public programMode currentMode;

	// Use this for initialization
	void Start()
	{
		cameraControls = GetComponent<CameraPositionControl>();

		if (currentMode == programMode.autodetect)
		{
			// First, try to detect the presence of the hololens
			#if UNITY_WSA
				VRSettings.enabled = false;
				currentMode = programMode.hololens;
			#endif

			if (currentMode == programMode.autodetect && VRSettings.enabled && VRDevice.isPresent)
			{
				currentMode = programMode.unityVR;
			}
		}
		

		switch(currentMode)
		{
			case programMode.autodetect:
			case programMode.desktop:
				// Adjust Cameras
				ARCamera.SetActive(false);
				VRCamera.SetActive(false);				
				DesktopCamera.SetActive(true);
				cameraControls.cameraHolder = DesktopCamera.transform;
				cameraControls.enabled = false;
				break;

			case programMode.hololens:
				DesktopCamera.SetActive(false);
				VRCamera.SetActive(false);
				ARCamera.SetActive(true);
				cameraControls.cameraHolder = ARCamera.transform;
				break;

			case programMode.unityVR:
				// Adjust Cameras
				ARCamera.SetActive(false);
				DesktopCamera.SetActive(false);
				VRCamera.SetActive(true);
				cameraControls.cameraHolder = VRCamera.transform;
				break;
		}
	}
	
	// Update is called once per frame
	void Update()
	{
	
	}
}
