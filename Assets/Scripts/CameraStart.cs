using UnityEngine;
using System.Collections;
using UnityEngine.VR;

public class CameraStart : MonoBehaviour
{
	public float zOffset = -90f;

	void Start ()
	{
		if (VRSettings.enabled)
			transform.position = new Vector3 (transform.position.x, transform.position.y, zOffset);
	}
	
}
