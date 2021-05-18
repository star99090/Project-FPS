using UnityEngine;
using System.Collections;

public class BulletScriptBefore : MonoBehaviour
{

	[Range(0.1f, 0.5f)]
	[Tooltip("총알 파괴 예정 시간")]
	public float destroyAfter;

	[Header("Impact Effect Prefab")]
	public Transform bloodImpactPrefabs;
	public Transform metalImpactPrefabs;
	public Transform dirtImpactPrefabs;
	public Transform concreteImpactPrefabs;

	private void Start()
	{
		// 생성된 이후 예정 시간이 지나면 총알 자동 파괴
		StartCoroutine(DestroyAfter());
	}
	
	private void OnCollisionEnter (Collision collision) 
	{
		// Metal 태그 오브젝트
		if (collision.transform.tag == "Metal")
		{
			BoltNetwork.Instantiate(metalImpactPrefabs.gameObject, transform.position,
				Quaternion.LookRotation(collision.contacts[0].normal));
			Destroy(gameObject);
		}

		// Dirt 태그 오브젝트
		if (collision.transform.tag == "Dirt")
		{
			BoltNetwork.Instantiate(dirtImpactPrefabs.gameObject, transform.position,
				Quaternion.LookRotation(collision.contacts[0].normal));
			Destroy(gameObject);
		}

		// Blood 태그 오브젝트
		if (collision.transform.tag == "Blood")
		{
			Instantiate(bloodImpactPrefabs.gameObject, transform.position,
				Quaternion.LookRotation(collision.contacts[0].normal));
			Destroy(gameObject);
		}

		// Concrete 태그 오브젝트
		if (collision.transform.tag == "Concrete")
		{
			BoltNetwork.Instantiate(concreteImpactPrefabs.gameObject, transform.position,
				Quaternion.LookRotation(collision.contacts[0].normal));
			Destroy(gameObject);
		}
	}

	private IEnumerator DestroyAfter() 
	{
		yield return new WaitForSeconds (destroyAfter);

		Destroy(gameObject);
	}
}