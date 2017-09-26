using UnityEngine;
using System.Collections;

[AddComponentMenu("Shepherd/Switch Neutral Position")]
public class SwitchNeutralPosition : MonoBehaviour 
{
	public Transform newNeutral;

	void OnTriggerEnter(Collider triggeree)
	{
		// Check if the object in position switcher is a sheep
		var toSwitch = triggeree.GetComponent<Sheep>();
		if (toSwitch == null)
		{
			return;
		}

		toSwitch.runMagnet = newNeutral;
	}
}
