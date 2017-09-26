using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("Shepherd/Camera Position Control")]
public class CameraPositionControl : MonoBehaviour 
{
	public Transform cameraHolder;
	public List<Transform> viewPoints = new List<Transform>();
	public float translationSpeed = 1f;

	private int lastViewPoint = -1;

	// Use this for initialization
	void Start ()
	{
		SwitchViewPoint();
	}

	void Update()
	{
		// Switch viewpoints on space
		if (Input.GetKeyDown(KeyCode.Space))
		{
			SwitchViewPoint();
		}

		if (Input.GetKey(KeyCode.UpArrow))
		{
			cameraHolder.Translate(new Vector3(0, -translationSpeed, 0), Space.World);
		}
		if (Input.GetKey(KeyCode.DownArrow))
		{
			cameraHolder.Translate(new Vector3(0, translationSpeed, 0), Space.World);
		}
		if (Input.GetKey(KeyCode.LeftArrow))
		{
			cameraHolder.Translate(new Vector3(translationSpeed, 0, 0), Space.World);
		}
		if (Input.GetKey(KeyCode.RightArrow))
		{
			cameraHolder.Translate(new Vector3(-translationSpeed, 0, 0), Space.World);
		}
		if (Input.GetKey(KeyCode.Home))
		{
			cameraHolder.Translate(new Vector3(0, 0, -translationSpeed), Space.World);
		}
		if (Input.GetKey(KeyCode.End))
		{
			cameraHolder.Translate(new Vector3(0, 0, translationSpeed), Space.World);
		}
	}

	void SwitchViewPoint()
	{
		int newViewPoint = (lastViewPoint + 1) % viewPoints.Count;

		// Undo last viewpoint's translation and apply new one
		var cameraPos = cameraHolder.localPosition;
		if (lastViewPoint >= 0)
		{
			cameraPos -= (viewPoints[lastViewPoint].position - transform.position);
		}
		// Rotate this local offset
		cameraPos = Quaternion.Inverse(cameraHolder.transform.localRotation) * cameraPos;
		cameraPos = viewPoints[newViewPoint].rotation * cameraPos;
		cameraPos += (viewPoints[newViewPoint].position - transform.position);
		cameraHolder.localPosition = cameraPos;
		cameraHolder.transform.rotation = viewPoints[newViewPoint].rotation;

		lastViewPoint = newViewPoint;
	}
}
