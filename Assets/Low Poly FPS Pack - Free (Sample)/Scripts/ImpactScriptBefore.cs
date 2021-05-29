using UnityEngine;
using System.Collections;
using Bolt;

public class ImpactScriptBefore : EntityBehaviour<IImpactState>
{

	[Tooltip("임팩트 파괴 예정 시간")]
	public float despawnTimer = 3.0f;

	[Header("Audio")]
	public AudioClip[] impactSounds;
	public AudioSource audioSource;

	private void Start ()
	{
		StartCoroutine(DespawnTimer());

		// 임팩트 사운드 랜덤 대입
		audioSource.clip = impactSounds
			[Random.Range(0, impactSounds.Length)];

		audioSource.Play();
	}
	
	private IEnumerator DespawnTimer()
	{
		yield return new WaitForSeconds (despawnTimer);

		Destroy(gameObject);
	}
}