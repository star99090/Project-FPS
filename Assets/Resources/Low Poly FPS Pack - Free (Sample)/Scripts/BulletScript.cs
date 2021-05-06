using UnityEngine;
using System.Collections;

public class BulletScript : MonoBehaviour
{

	[Range(0.1f, 0.5f)]
	[Tooltip("총알 파괴 예정 시간")]
	public float destroyAfter;

	[Header("Impact Effect Prefab")]
	public Transform [] metalImpactPrefabs;

	private void Start()
	{
		// 생성된 이후 예정 시간이 지나면 총알 자동 파괴
		StartCoroutine(DestroyAfter());
	}

	private void OnCollisionEnter (Collision collision) 
	{
		Destroy(gameObject);

		// 총알이 Metal 태그의 오브젝트와 만났을 때
		if (collision.transform.tag == "Metal")
		{
			BoltNetwork.Instantiate(metalImpactPrefabs[0].gameObject, transform.position,
				Quaternion.LookRotation(collision.contacts[0].normal));
		}
		
		Destroy(gameObject);
	}

	private IEnumerator DestroyAfter() 
	{
		yield return new WaitForSeconds (destroyAfter);

		Destroy(gameObject);
	}
}