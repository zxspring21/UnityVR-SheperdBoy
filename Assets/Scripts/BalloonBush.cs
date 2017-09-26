using UnityEngine;
using System.Collections;

[AddComponentMenu("Shepherd/Balloon Bush")]
public class BalloonBush : MonoBehaviour 
{

	private Animator bushAnimator;
	// Use this for initialization
	void Start ()
	{
		bushAnimator = GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update ()
	{
	
	}

	void OnTriggerEnter(Collider triggeree)
	{
		// Check if the object in balloon bush's eating range is a sheep
		var toSheer = triggeree.GetComponent<Sheep>();
		if (toSheer == null)
		{
			return;
		}

		// Set animation flags
		var sheepAnimator = toSheer.GetComponent<Animator>();
		sheepAnimator.SetTrigger("Eat");
		sheepAnimator.SetBool("Cinematic", true);
		Destroy(toSheer.GetComponent<NavMeshAgent>());
		Destroy(toSheer.GetComponent<NavAgentToAnimator>());

		// Sheep are floaty
		toSheer.gameObject.AddComponent<FloatySheep>();

		// Remove sheep component
		Destroy(toSheer);

		// Bush is chewed upon
		bushAnimator.SetTrigger("Eat");
	}
}
