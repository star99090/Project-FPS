using UnityEngine;
using System.Collections;
using Bolt;
using static NetworkManager;

public class ProjectileScript : EntityBehaviour<IProjectileState>
{
	[Tooltip("일정한 힘 사용 유무")]
	public bool useConstantForce;
	[Tooltip("포탄의 이동속도")]
	public float constantForceSpeed;
	[Tooltip("폭발 후 포탄 파괴까지 걸리는 시간")]
	public float explodeAfter;
	private bool hasStartedExplode;

	[Header("Explosion Prefabs")]
	public Transform explosionPrefab;

	[Header("Customizable Options")]
	[Tooltip("포탄 발사 힘")]
	public float force = 5000f;
	[Tooltip("자동 파괴까지 걸리는 시간")]
	public float despawnTime = 30f;

	[Header("Explosion Options")]
	[Tooltip("폭발 반지름")]
	public float radius = 50.0F;

	[Header("Rocket Launcher Projectile")]
	public ParticleSystem smokeParticles;
	public ParticleSystem flameParticles;
	[Tooltip("자동 공중 파괴까지 걸리는 시간")]
	public float destroyDelay;

	private void Start()
	{
		// 일정하지 않은 힘으로 발사(그레네이드 런처)
		if (!useConstantForce) 
		{
			GetComponent<Rigidbody> ().AddForce 
				(gameObject.transform.forward * force);
		}

		StartCoroutine (DestroyTimer());
	}
		
	private void FixedUpdate()
	{
		// 포물선 이동으로 바라보게 함
		if (GetComponent<Rigidbody>().velocity != Vector3.zero)
			GetComponent<Rigidbody>().rotation =
				Quaternion.LookRotation(GetComponent<Rigidbody>().velocity);

		// 일정한 힘 사용
		if (useConstantForce == true && !hasStartedExplode)
		{
			// 일정한 힘으로 일정한 방향에 발사(로켓)
			GetComponent<Rigidbody>().AddForce 
				(gameObject.transform.forward * constantForceSpeed);

			StartCoroutine (ExplodeSelf());

			hasStartedExplode = true;
		}
	}

	// 공중에서 자동 폭파
	private IEnumerator ExplodeSelf() 
	{
		yield return new WaitForSeconds (explodeAfter);

		BoltNetwork.Instantiate(explosionPrefab.gameObject, transform.position, transform.rotation);
		
		gameObject.GetComponent<MeshRenderer> ().enabled = false;
		gameObject.GetComponent<Rigidbody> ().isKinematic = true;
		gameObject.GetComponent<BoxCollider>().isTrigger = true;

		// 파괴 전에 파티클을 중지하고 플레이가 마무리되게 함
		flameParticles.GetComponent<ParticleSystem>().Stop();
		smokeParticles.GetComponent<ParticleSystem>().Stop();

		yield return new WaitForSeconds (destroyDelay);

		Destroy(gameObject);
	}

	private IEnumerator DestroyTimer() 
	{
		yield return new WaitForSeconds (despawnTime);
		Destroy(gameObject);
	}

	private IEnumerator DestroyTimerAfterCollision() 
	{
		yield return new WaitForSeconds (destroyDelay);
		Destroy(gameObject);
	}

	// 포탄과 무언가 접촉 시
	private void OnCollisionEnter (Collision collision) 
	{
		gameObject.GetComponent<MeshRenderer> ().enabled = false;
		gameObject.GetComponent<Rigidbody> ().isKinematic = true;
		gameObject.GetComponent<BoxCollider>().isTrigger = true;

		StopAllCoroutines();

		if (useConstantForce)
		{
			flameParticles.GetComponent<ParticleSystem>().Stop();
			smokeParticles.GetComponent<ParticleSystem>().Stop();
		}

		StartCoroutine (DestroyTimerAfterCollision ());

		// 부딛힌 물체의 위치에서 폭발 프리팹 생성
		BoltNetwork.Instantiate(explosionPrefab.gameObject, collision.contacts[0].point,
		Quaternion.LookRotation(collision.contacts[0].normal));

		Vector3 explosionPos = transform.position;
		Collider[] colliders = Physics.OverlapSphere(explosionPos, radius);
		foreach (Collider hit in colliders)
		{
			if(hit.gameObject.tag == "FPSPlayer")
            {
				var evnt = PlayerHitEvent.Create();
				evnt.targetEntity = hit.gameObject.GetComponent<BoltEntity>();

				if(useConstantForce)
					evnt.damage = Random.Range(95, 130);
				else
					evnt.damage = Random.Range(90, 120);

				var players = NM.players;
				for (int i = 0; i < players.Count; i++)
				{
					if (players[i].IsOwner)
					{
						evnt.attackerEntity = players[i];
						evnt.attacker = players[i].gameObject.GetComponent<Player>().nicknameText.text;
					}
				}
				evnt.Send();
			}
		}
	}
}