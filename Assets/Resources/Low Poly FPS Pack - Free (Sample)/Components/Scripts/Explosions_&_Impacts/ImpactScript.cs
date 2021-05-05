using UnityEngine;
using System.Collections;

// ----- Low Poly FPS Pack Free Version -----
public class ImpactScript : Bolt.EntityBehaviour<IMetalImpactState> {

	[Header("Impact Despawn Timer")]
	//How long before the impact is destroyed
	public float despawnTimer = 3.0f;

	[Header("Audio")]
	public AudioClip[] impactSounds;
	public AudioSource audioSource;

	private void Start () {
		// Start the despawn timer
		StartCoroutine(DespawnTimer());

		//Get a random impact sound from the array
		audioSource.clip = impactSounds
			[Random.Range(0, impactSounds.Length)];
		//Play the random impact sound
		audioSource.Play();
	}
	
	private IEnumerator DespawnTimer() {
		//Wait for set amount of time
		yield return new WaitForSeconds (despawnTimer);
		//Destroy the impact gameobject
		Destroy(gameObject);
	}
	/*
	IEnumerator DestroyRequest()
    {
		yield return new WaitForSeconds(despawnTimer);
		var evnt = DestroyRequestEvent.Create();
		evnt.Entity = GetComponent<BoltEntity>();
		evnt.Send();
    }*/
}
// ----- Low Poly FPS Pack Free Version -----