using UnityEngine;

[AddComponentMenu("Shepherd/Floaty Sheep")]
public class FloatySheep : MonoBehaviour 
{
	protected float delay = 1.25f;
	protected float floatAcceleration = 9.8f;
	protected float currentFloatSpeed = 0f;
	protected float destroyDelay = 12f;

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (delay > 0)
		{
			delay -= Time.deltaTime;
		}
		else
		{
			currentFloatSpeed += floatAcceleration * Time.deltaTime;
			transform.Translate(0, currentFloatSpeed * Time.deltaTime, 0, Space.World);
		}
		destroyDelay -= Time.deltaTime;
		if (destroyDelay < 0)
		{
			Destroy(gameObject);
		}
	}
}
