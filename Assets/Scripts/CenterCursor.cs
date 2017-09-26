using UnityEngine;
using System.Collections;

[AddComponentMenu("Shepherd/Center Cursor")]
public class CenterCursor : MonoBehaviour 
{
	private Camera cameraSource;
	public Transform reticle;

	void Start () 
	{
		// This is on a camera
		cameraSource = GetComponent<Camera>();

		if (reticle == null)
		{
			reticle = GameObject.FindObjectOfType<ControlReticle>().transform;
		}
	}
	
	// Update is called once per frame
	void Update () 
	{
		// Convert the mouse position to a ray and raycast to the terrain
		var centerRay = cameraSource.ScreenPointToRay(new Vector3(Screen.width * .5f, Screen.height * .5f));
		var hitInfo = new RaycastHit();
		var terrainOnlyMask = 1 << 8;
		if (Physics.Raycast(centerRay, out hitInfo, 1000, terrainOnlyMask))
		{
			// If we are on the navmesh, we update our position
			var navHitInfo = new NavMeshHit();
			if (NavMesh.SamplePosition(hitInfo.point, out navHitInfo, 5, NavMesh.AllAreas))
			{
				reticle.position = navHitInfo.position;
			}
		}
	}
}
