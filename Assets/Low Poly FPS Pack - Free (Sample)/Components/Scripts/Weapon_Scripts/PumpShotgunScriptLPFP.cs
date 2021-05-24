using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Bolt;

public class PumpShotgunScriptLPFP : EntityBehaviour<IFPSPlayerState>
{
	public Animator anim;

	[Header("Gun Camera")]
	public Camera gunCamera;

	[Header("Gun Camera Options")]
	[Tooltip("조준 시 카메라 변경 속도")]
	public float fovSpeed = 15.0f;

	[Tooltip("카메라 시야 기본 값")]
	public float defaultFov = 40.0f;

	private float aimFov;

	[Header("Weapon Name UI")]
	[Tooltip("총기 이름")]
	public string weaponName;

	[Header("Weapon Attachments (Only use one scope attachment)")]
	[Space(10)]
	// 첫 번째 스코프 토글
	public bool scope1;
	public Sprite scope1Texture;
	public float scope1TextureSize = 0.0045f;
	[Range(5, 40)]
	public float scope1AimFOV = 10;
	[Space(10)]

	// 두 번째 스코프 토글
	public bool scope2;
	public Sprite scope2Texture;
	public float scope2TextureSize = 0.01f;
	[Range(5, 40)]
	public float scope2AimFOV = 25;
	[Space(10)]

	// 세 번째 스코프 토글
	public bool scope3;
	public Sprite scope3Texture;
	public float scope3TextureSize = 0.006f;
	[Range(5, 40)]
	public float scope3AimFOV = 20;
	[Space(10)]

	// 네 번째 스코프 토글
	public bool scope4;
	public Sprite scope4Texture;
	public float scope4TextureSize = 0.0025f;
	[Range(5, 40)]
	public float scope4AimFOV = 12;
	[Space(10)]

	// 총기 자체 가늠쇠
	public bool ironSights;
	public bool alwaysShowIronSights;
	[Range(5, 40)]
	public float ironSightsAimFOV = 16;

	//Weapon attachments components
	[System.Serializable]
	public class weaponAttachmentRenderers 
	{
		[Header("Scope Model Renderers")]
		[Space(10)]
		//All attachment renderer components
		public SkinnedMeshRenderer scope1Renderer;
		public SkinnedMeshRenderer scope2Renderer;
		public SkinnedMeshRenderer scope3Renderer;
		public SkinnedMeshRenderer scope4Renderer;
		public SkinnedMeshRenderer ironSightsRenderer;
		[Header("Scope Sight Mesh Renderers")]
		[Space(10)]
		//Scope render meshes
		public GameObject scope1RenderMesh;
		public GameObject scope2RenderMesh;
		public GameObject scope3RenderMesh;
		public GameObject scope4RenderMesh;
		[Header("Scope Sight Sprite Renderers")]
		[Space(10)]
		//Scope sight textures
		public SpriteRenderer scope1SpriteRenderer;
		public SpriteRenderer scope2SpriteRenderer;
		public SpriteRenderer scope3SpriteRenderer;
		public SpriteRenderer scope4SpriteRenderer;
	}
	public weaponAttachmentRenderers WeaponAttachmentRenderers;

	//Used for fire rate

	//How fast the weapon fires, higher value means faster rate of fire
	[Header("Weapon Settings")]
	[Tooltip("발사 딜레이 조절")]
	public float fireRate;
	private float lastFired;

	[Tooltip("최대 탄 수")]
	public int ammo;
	private int currentAmmo;
	private bool outOfAmmo;
	private bool fullAmmo;

	[Header("Bullet Settings")]
	[Tooltip("총탄 발사 힘")]
	public float bulletForce = 400f;

	[Tooltip("데미지 중간 값")]
	public int damage = 20;

	[Header("Muzzleflash Settings")]
	public ParticleSystem muzzleParticles;
	public ParticleSystem sparkParticles;
	public int minSparkEmission = 1;
	public int maxSparkEmission = 7;
	private int randomMuzzleflashValue;

	[Header("Muzzleflash Light Settings")]
	public Light muzzleFlashLight;
	public float lightDuration = 0.02f;

	[Header("Audio Source")]
	public AudioSource mainAudioSource;
	public AudioSource shootAudioSource;

	[Header("UI Components")]
	public Text currentWeaponText;
	public Text currentAmmoText;
	public Text totalAmmoText;
	[SerializeField] private Text attacker;

	[System.Serializable]
	public class prefabs
	{  
		[Header("Prefabs")]
		public Transform bulletPrefab;
		public Transform casingPrefab;
	}
	public prefabs Prefabs;
	
	[System.Serializable]
	public class spawnpoints
	{  
		[Header("Spawnpoints")]
		public float casingDelayTimer;
		public Transform casingSpawnPoint;
		public Transform[] bulletSpawnPoint;
		[Range(-10, 10)]
		public float bulletSpawnPointMinRotation = -5.0f;
		[Range(-10, 10)]
		public float bulletSpawnPointMaxRotation = 5.0f;
	}
	public spawnpoints Spawnpoints;

	[System.Serializable]
	public class soundClips
	{
		public AudioClip shootSound;
		public AudioClip takeOutSound;
		public AudioClip aimSound;
	}
	public soundClips SoundClips;

	private bool soundHasPlayed = false;

	[Header("Other Settings")]
	[SerializeField] private BoltEntity myEntity;
	[SerializeField] private GameObject myCharacterModel;
	public Transform bloodImpactPrefabs;
	public bool isCurrentWeapon;

	private bool isDraw = true;
	private bool isAutoReloading;
	private bool isReloadingAnim;
	private bool isReloading = false;
	private bool isRunning;
	private bool isAiming;

	private void Awake () 
	{
		anim = GetComponent<Animator>();
		currentAmmo = ammo;
		muzzleFlashLight.enabled = false;

		// 무기 부착물
		// Scope1 활성화
		if (scope1 == true && WeaponAttachmentRenderers.scope1Renderer != null)
		{
			WeaponAttachmentRenderers.scope1Renderer.GetComponent
			<SkinnedMeshRenderer>().enabled = true;

			WeaponAttachmentRenderers.scope1RenderMesh.SetActive(true);

			WeaponAttachmentRenderers.scope1SpriteRenderer.GetComponent
				<SpriteRenderer>().sprite = scope1Texture;

			WeaponAttachmentRenderers.scope1SpriteRenderer.transform.localScale = new Vector3
				(scope1TextureSize, scope1TextureSize, scope1TextureSize);
		}
		// Scope1 비활성화
		else if (WeaponAttachmentRenderers.scope1Renderer != null)
		{
			WeaponAttachmentRenderers.scope1Renderer.GetComponent<
			SkinnedMeshRenderer>().enabled = false;

			WeaponAttachmentRenderers.scope1RenderMesh.SetActive(false);
		}

		// Scope2 활성화
		if (scope2 == true && WeaponAttachmentRenderers.scope2Renderer != null)
		{
			WeaponAttachmentRenderers.scope2Renderer.GetComponent
			<SkinnedMeshRenderer>().enabled = true;

			WeaponAttachmentRenderers.scope2RenderMesh.SetActive(true);

			WeaponAttachmentRenderers.scope2SpriteRenderer.GetComponent
			<SpriteRenderer>().sprite = scope2Texture;

			WeaponAttachmentRenderers.scope2SpriteRenderer.transform.localScale = new Vector3
				(scope2TextureSize, scope2TextureSize, scope2TextureSize);
		}
		// Scope2 비활성화
		else if (WeaponAttachmentRenderers.scope2Renderer != null)
		{
			WeaponAttachmentRenderers.scope2Renderer.GetComponent
			<SkinnedMeshRenderer>().enabled = false;

			WeaponAttachmentRenderers.scope2RenderMesh.SetActive(false);
		}

		// Scope3 활성화
		if (scope3 == true && WeaponAttachmentRenderers.scope3Renderer != null)
		{
			WeaponAttachmentRenderers.scope3Renderer.GetComponent
			<SkinnedMeshRenderer>().enabled = true;

			WeaponAttachmentRenderers.scope3RenderMesh.SetActive(true);

			WeaponAttachmentRenderers.scope3SpriteRenderer.GetComponent
			<SpriteRenderer>().sprite = scope3Texture;

			WeaponAttachmentRenderers.scope3SpriteRenderer.transform.localScale = new Vector3
				(scope3TextureSize, scope3TextureSize, scope3TextureSize);
		}
		// Scope3 비활성화
		else if (WeaponAttachmentRenderers.scope3Renderer != null)
		{
			WeaponAttachmentRenderers.scope3Renderer.GetComponent
			<SkinnedMeshRenderer>().enabled = false;

			WeaponAttachmentRenderers.scope3RenderMesh.SetActive(false);
		}

		// Scope4 활성화
		if (scope4 == true && WeaponAttachmentRenderers.scope4Renderer != null)
		{
			WeaponAttachmentRenderers.scope4Renderer.GetComponent
			<SkinnedMeshRenderer>().enabled = true;

			WeaponAttachmentRenderers.scope4RenderMesh.SetActive(true);

			WeaponAttachmentRenderers.scope4SpriteRenderer.GetComponent
			<SpriteRenderer>().sprite = scope4Texture;

			WeaponAttachmentRenderers.scope4SpriteRenderer.transform.localScale = new Vector3
				(scope4TextureSize, scope4TextureSize, scope4TextureSize);
		}
		// Scope4 비활성화
		else if (WeaponAttachmentRenderers.scope4Renderer != null)
		{
			WeaponAttachmentRenderers.scope4Renderer.GetComponent
			<SkinnedMeshRenderer>().enabled = false;

			WeaponAttachmentRenderers.scope4RenderMesh.SetActive(false);
		}

		// 항상 가늠쇠가 있다면
		if (alwaysShowIronSights == true && WeaponAttachmentRenderers.ironSightsRenderer != null)
		{
			WeaponAttachmentRenderers.ironSightsRenderer.GetComponent
			<SkinnedMeshRenderer>().enabled = true;
		}

		// 가늠쇠가 있다면
		if (ironSights == true && WeaponAttachmentRenderers.ironSightsRenderer != null)
		{
			WeaponAttachmentRenderers.ironSightsRenderer.GetComponent
			<SkinnedMeshRenderer>().enabled = true;
		}
		// 항상 가늠쇠 보이기를 끄고 가늠쇠 자체를 꺼뒀다면
		else if (!alwaysShowIronSights &&
		  WeaponAttachmentRenderers.ironSightsRenderer != null)
		{
			WeaponAttachmentRenderers.ironSightsRenderer.GetComponent
			<SkinnedMeshRenderer>().enabled = false;
		}
	}

	private void Start () 
	{
		currentWeaponText.text = weaponName;
		totalAmmoText.text = ammo.ToString();
		shootAudioSource.clip = SoundClips.shootSound;
	}

	public override void Attached()
	{
		state.AddCallback("MuzzleParticleTrigger", MuzzleParticleCallback);
		state.AddCallback("SparkParticleTrigger", SparkParticleCallback);
		state.OnMuzzleParticleTrigger += MuzzleParticleCallback;
		state.OnSparkParticleTrigger += SparkParticleCallback;
	}

	void MuzzleParticleCallback()
	{
		if (isCurrentWeapon)
		{
			muzzleParticles.Emit(1);
			StartCoroutine(MuzzleFlashLight());
		}
	}

	void SparkParticleCallback() => sparkParticles.Emit(Random.Range(minSparkEmission, maxSparkEmission));

	void PlayerHitCheck(Transform bulletSpawnPos)
	{
		Physics.Raycast(bulletSpawnPos.position, bulletSpawnPos.forward, out RaycastHit hit);
		if (hit.collider != null && hit.collider.gameObject.CompareTag("FPSPlayer"))
		{
			Instantiate(bloodImpactPrefabs.gameObject, hit.point,
			   Quaternion.LookRotation(transform.position));

			var evnt = PlayerHitEvent.Create();
			evnt.attacker = attacker.text;
			evnt.targetEntity = hit.collider.gameObject.GetComponent<BoltEntity>();
			evnt.damage = Random.Range(damage - 2, damage + 2);
			evnt.attackerEntity = myEntity;
			evnt.Send();

		}
	}
	
	private void Update () 
	{
		if (!entity.IsOwner) return;

		if (anim.GetCurrentAnimatorStateInfo(0).IsName("Draw")
			&& anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
			isDraw = false;

		// 우클릭 조준 시 카메라 셋팅
		if (Input.GetButton("Fire2") && !isReloadingAnim && !isRunning & !isReloading && !isDraw) 
		{
			if (ironSights == true)
			{
				aimFov = ironSightsAimFOV;
				anim.SetBool("Aim", true);
			}
			if (scope1 == true)
			{
				aimFov = scope1AimFOV;
				anim.SetBool("AimScope1", true);
				WeaponAttachmentRenderers.scope1SpriteRenderer.GetComponent
					<SpriteRenderer>().enabled = true;
			}
			if (scope2 == true)
			{
				aimFov = scope2AimFOV;
				anim.SetBool("AimScope2", true);
				WeaponAttachmentRenderers.scope2SpriteRenderer.GetComponent
				 <SpriteRenderer>().enabled = true;
			}
			if (scope3 == true)
			{
				aimFov = scope3AimFOV;
				anim.SetBool("AimScope3", true);
				WeaponAttachmentRenderers.scope3SpriteRenderer.GetComponent
				<SpriteRenderer>().enabled = true;
			}
			if (scope4 == true)
			{
				aimFov = scope4AimFOV;
				anim.SetBool("AimScope4", true);
				WeaponAttachmentRenderers.scope4SpriteRenderer.GetComponent
				<SpriteRenderer>().enabled = true;
			}

			isAiming = true;

			gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView,
				aimFov, fovSpeed * Time.deltaTime);

			if (!soundHasPlayed) 
			{
				mainAudioSource.clip = SoundClips.aimSound;
				mainAudioSource.Play ();
	
				soundHasPlayed = true;
			}
		}
		// 우클릭 해제
		else
		{
			gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView,
				defaultFov,fovSpeed * Time.deltaTime);

			isAiming = false;

			if (ironSights == true)
				anim.SetBool("Aim", false);
			if (scope1 == true)
			{
				anim.SetBool("AimScope1", false);
				WeaponAttachmentRenderers.scope1SpriteRenderer.GetComponent
					<SpriteRenderer>().enabled = false;
			}
			if (scope2 == true)
			{
				anim.SetBool("AimScope2", false);
				WeaponAttachmentRenderers.scope2SpriteRenderer.GetComponent
				<SpriteRenderer>().enabled = false;
			}
			if (scope3 == true)
			{
				anim.SetBool("AimScope3", false);
				WeaponAttachmentRenderers.scope3SpriteRenderer.GetComponent
				<SpriteRenderer>().enabled = false;
			}
			if (scope4 == true)
			{
				anim.SetBool("AimScope4", false);
				WeaponAttachmentRenderers.scope4SpriteRenderer.GetComponent
				<SpriteRenderer>().enabled = false;
			}

			soundHasPlayed = false;
		}

		// 발사 이펙트 빈도 수 지정
		randomMuzzleflashValue = Random.Range(1, 2);

		// 현재 탄 수 동기화
		currentAmmoText.text = currentAmmo.ToString ();

		// 탄이 꽉 차있는 상태인지
		if (currentAmmo == ammo)
			fullAmmo = true;
		else
			fullAmmo = false;

		// 현재 재장전 애니메이션 진행 중인지 확인
		AnimationCheck();

		// 탄 다 썼을 때
		if (currentAmmo == 0 && !isReloadingAnim && !isAutoReloading) 
		{
			isAutoReloading = true;
			currentWeaponText.text = "OUT OF AMMO";
			
			Reload();
		}
			
		//Fire
		if (Input.GetMouseButton (0) && !outOfAmmo && !isReloadingAnim
			&& !isRunning && !isReloading && isCurrentWeapon && !isDraw) 
		{
			if (Time.time - lastFired > 1 / fireRate) 
			{
				lastFired = Time.time;

				myCharacterModel.GetComponent<CharacterAnimation>().FireAnim();

				// 탄 수 감소
				currentAmmo -= 1;

				shootAudioSource.clip = SoundClips.shootSound;
				shootAudioSource.Play ();

				if (randomMuzzleflashValue == 1)
				{
					state.SparkParticleTrigger();
					state.MuzzleParticleTrigger();
				}

				// 일반 사격 모드
				if (!isAiming)
					anim.Play ("Fire", 0, 0f);

				// 조준 사격 모드
				else
				{
					if (ironSights == true)
						anim.Play("Aim Fire", 0, 0f);
					if (scope1 == true)
						anim.Play("Aim Fire Scope 1", 0, 0f);
					if (scope2 == true)
						anim.Play("Aim Fire Scope 2", 0, 0f);
					if (scope3 == true)
						anim.Play("Aim Fire Scope 3", 0, 0f);
					if (scope4 == true)
						anim.Play("Aim Fire Scope 4", 0, 0f);
				}

				// 총알 생성 배열
				for (int i = 0; i < Spawnpoints.bulletSpawnPoint.Length; i++)
				{
					// 모든 총알의 Rotation을 min 부터 max 값까지 랜덤하게 부여
					Spawnpoints.bulletSpawnPoint[i].transform.localRotation = Quaternion.Euler(
						Random.Range(Spawnpoints.bulletSpawnPointMinRotation, Spawnpoints.bulletSpawnPointMaxRotation),
						Random.Range(Spawnpoints.bulletSpawnPointMinRotation, Spawnpoints.bulletSpawnPointMaxRotation),
						0);
					
					// 총알 생성
					var bullet = (Transform)Instantiate (
						Prefabs.bulletPrefab,
						Spawnpoints.bulletSpawnPoint [i].transform.position,
						Spawnpoints.bulletSpawnPoint [i].transform.rotation);

					// 총알에 힘 싣기
					bullet.GetComponent<Rigidbody>().velocity = 
						bullet.transform.forward * bulletForce;

					PlayerHitCheck(Spawnpoints.bulletSpawnPoint[i].transform);
				}

				StartCoroutine (CasingDelay ());
			}
		}

		// 재장전
		if (Input.GetKeyDown(KeyCode.R) && !isReloadingAnim
			&& isCurrentWeapon && !isDraw && !fullAmmo)
			Reload();

		// 걷기
		if (Input.GetKey (KeyCode.W) && !isRunning || 
			Input.GetKey (KeyCode.A) && !isRunning || 
			Input.GetKey (KeyCode.S) && !isRunning || 
			Input.GetKey (KeyCode.D) && !isRunning) 
			anim.SetBool ("Walk", true);
		else 
			anim.SetBool ("Walk", false);

		// W와 Left Shift를 누르면 달리기
		if ((Input.GetKey (KeyCode.W) && Input.GetKey (KeyCode.LeftShift))) 
			isRunning = true;
		else 
			isRunning = false;

		// 달리기 애니메이션 설정
		if (isRunning == true)
			anim.SetBool ("Run", true);
		else
			anim.SetBool ("Run", false);
	}

	private IEnumerator CasingDelay()
	{
		yield return new WaitForSeconds (Spawnpoints.casingDelayTimer);

		Instantiate (Prefabs.casingPrefab, 
			Spawnpoints.casingSpawnPoint.transform.position, 
			Spawnpoints.casingSpawnPoint.transform.rotation);
	}

	// 장전
	private void Reload()
	{
		isReloading = true;
		myCharacterModel.GetComponent<CharacterAnimation>().ReloadAnim();
		
		if (isCurrentWeapon)
		{
			switch (currentAmmo)
			{
				case 5:
					anim.Play("Reload Open 5", 0, 0f);
					break;
				case 4:
					anim.Play("Reload Open 4", 0, 0f);
					break;
				case 3:
					anim.Play("Reload Open 3", 0, 0f);
					break;
				case 2:
					anim.Play("Reload Open 2", 0, 0f);
					break;
				case 1:
					anim.Play("Reload Open 1", 0, 0f);
					break;
				case 0:
					anim.Play("Reload Open", 0, 0f);
					break;
			}
			//anim.Play("Reload Open", 0, 0f);
		}
		
		// 장전 과정
		Invoke("ReloadSub", 1.47f);
	}

	void ReloadSub()
	{
		StartCoroutine(AmmoPlus());
	}

	IEnumerator AmmoPlus()
	{
		currentWeaponText.text = weaponName;

		currentAmmo++;

        for (; currentAmmo < ammo; currentAmmo++)
        {
			yield return new WaitForSeconds(0.7f);
		}

		yield return new WaitForSeconds(0.867f);
		
		// 재장전 완료
		currentAmmo = ammo;
		outOfAmmo = false;
		isReloading = false;
		isAutoReloading = false;
	}

	// 사격 시 총알의 불빛이 사라지는 시간 설정
	private IEnumerator MuzzleFlashLight () 
	{
		muzzleFlashLight.enabled = true;
		yield return new WaitForSeconds (lightDuration);
		muzzleFlashLight.enabled = false;
	}

	// 현재 재장전 애니메이션 진행 중인지 확인
	private void AnimationCheck () 
	{
		if (anim.GetCurrentAnimatorStateInfo(0).IsName ("Reload Open") || 
			anim.GetCurrentAnimatorStateInfo(0).IsName ("Reload Open") ||
			anim.GetCurrentAnimatorStateInfo(0).IsName ("Inser Shell") ||
			anim.GetCurrentAnimatorStateInfo(0).IsName ("Reload Close"))
			isReloadingAnim = true;
		else
			isReloadingAnim = false;
	}
}