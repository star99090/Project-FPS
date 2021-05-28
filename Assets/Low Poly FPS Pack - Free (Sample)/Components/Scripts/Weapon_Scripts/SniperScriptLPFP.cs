using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Bolt;
using static NetworkManager;

public class SniperScriptLPFP : EntityBehaviour<IFPSPlayerState>
{
	public Animator anim;

	[Header("Gun Camera")]
	public Camera gunCamera;

	[Header("Gun Camera Options")]
	[Tooltip("조준 시 카메라 변경 속도")]
	public float fovSpeed = 15.0f;

	[Tooltip("카메라 시야 기본 값")]
	public float defaultFov = 40.0f;

	public float aimFOV = 20.0f;

	[Header("Weapon Name UI")]
	[Tooltip("총기 이름")]
	public string weaponName;
	[SerializeField] GameObject aimPoint;

	[Header("Weapon Attachments")]
	public bool silencer;

	[System.Serializable]
	public class weaponAttachmentRenderers
	{
		public SkinnedMeshRenderer silencerRenderer;
	}
	public weaponAttachmentRenderers WeaponAttachmentRenderers;

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
	public float bulletForce = 400;

	[Tooltip("데미지 중간 값")]
	public int damage = 100;

	[Tooltip("탄피 자동 삭제에 걸리는 딜레이")]
	public float showBulletInMagDelay;

	[Tooltip("탄피 안의 총알 모델")]
	public SkinnedMeshRenderer bulletInMagRenderer;

	[Header("Hit Rate Settings")]
	[Tooltip("일반 사격 명중률 조정")]
	[Range(-5, 5)]
	public float normalHitRateMin = -5f;
	[Range(-5, 5)]
	public float normalHitRateMax = 5f;
	[Space(10)]

	[Header("Scope Settings")]
	public Material scopeRenderMaterial;
	public Color fadeColor;
	public Color defaultColor;

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
		public Transform casingSpawnPoint;
		public Transform bulletSpawnPoint;
	}
	public spawnpoints Spawnpoints;

	[System.Serializable]
	public class soundClips
	{
		public AudioClip shootSound;
		public AudioClip silencerShootSound;
		public AudioClip takeOutSound;
		public AudioClip reloadSoundOutOfAmmo;
		public AudioClip reloadSoundAmmoLeft;
		public AudioClip aimSound;
	}
	public soundClips SoundClips;

	private bool soundHasPlayed = false;

	[Header("Other Settings")]
	[SerializeField] private BoltEntity myEntity;
	[SerializeField] private GameObject myCharacterModel;
	[SerializeField] private Text attacker;
	public Transform bloodImpactPrefabs;
	public bool isCurrentWeapon;

	private bool isDraw = true;
	private bool isReloadingAnim;
	private bool isReloading = false;
	private bool isRunning;
	private bool isAiming;
	private bool isShooting;

	private void Awake()
	{
		anim = GetComponent<Animator>();
		currentAmmo = ammo;
		muzzleFlashLight.enabled = false;
		aimPoint.SetActive(false);

		// 소음기 장착
		if (silencer == true &&
			WeaponAttachmentRenderers.silencerRenderer != null)
		{
			WeaponAttachmentRenderers.silencerRenderer.GetComponent
			<SkinnedMeshRenderer>().enabled = true;
		}
		// 소음기 미장착
		else if (WeaponAttachmentRenderers.silencerRenderer != null)
		{
			WeaponAttachmentRenderers.silencerRenderer.GetComponent
			<SkinnedMeshRenderer>().enabled = false;
		}
	}

	private void Start()
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

	void PlayerHitCheck()
	{
		Physics.Raycast(Spawnpoints.bulletSpawnPoint.position, Spawnpoints.bulletSpawnPoint.forward, out RaycastHit hit);
		if (hit.collider != null && hit.collider.gameObject.CompareTag("FPSPlayer"))
		{
			Instantiate(bloodImpactPrefabs.gameObject, hit.point,
			   Quaternion.LookRotation(transform.position));

			var evnt = PlayerHitEvent.Create();
			evnt.attacker = attacker.text;
			evnt.targetEntity = hit.collider.gameObject.GetComponent<BoltEntity>();
			if (isAiming)
				evnt.damage = Random.Range(damage - 2, damage + 2);
			else
				evnt.damage = Random.Range(damage - 20, damage + 2);
			evnt.attackerEntity = myEntity;
			evnt.Send();
		}
	}

	private void Update()
	{
		if (!entity.IsOwner || NM.isResult) return;

		if (isDraw && !anim.GetCurrentAnimatorStateInfo(0).IsName("Draw"))
			isDraw = false;

		// 우클릭 조준 시 카메라 셋팅
		if (Input.GetButton("Fire2") && !isReloadingAnim && !isRunning & !isReloading && !isDraw)
		{
			isAiming = true;

			gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView,
				aimFOV, fovSpeed * Time.deltaTime);

			scopeRenderMaterial.color = defaultColor;

			anim.SetBool("Aim", true);

			if (!soundHasPlayed)
			{
				mainAudioSource.clip = SoundClips.aimSound;
				mainAudioSource.Play();

				soundHasPlayed = true;
			}
		}
		// 우클릭 해제
		else
		{
			gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView,
				defaultFov, fovSpeed * Time.deltaTime);

			scopeRenderMaterial.color = fadeColor;

			isAiming = false;

			anim.SetBool("Aim", false);

			soundHasPlayed = false;
		}

		// 발사 이펙트 빈도 수 지정
		randomMuzzleflashValue = Random.Range(1, 2);

		// 현재 탄 수 동기화
		currentAmmoText.text = currentAmmo.ToString();

		// 탄이 꽉 차있는 상태인지
		if (currentAmmo == ammo)
			fullAmmo = true;
		else
			fullAmmo = false;

		// 현재 재장전 애니메이션 진행 중인지 확인
		AnimationCheck();

		// 탄 다 썼을 때
		if (currentAmmo == 0 && isCurrentWeapon)
		{
			currentWeaponText.text = "OUT OF AMMO";

			outOfAmmo = true;

			if (!isReloadingAnim)
				StartCoroutine(AutoReload());
		}
		else 
		{
			currentWeaponText.text = weaponName;

			outOfAmmo = false;
		}

		// 자동 발사(좌클릭 유지)
		if (Input.GetMouseButton(0) && !outOfAmmo && !isReloadingAnim
			&& !isRunning && !isReloading && isCurrentWeapon && !isDraw)
		{
			if (Time.time - lastFired > 1 / fireRate)
			{
				lastFired = Time.time;

				myCharacterModel.GetComponent<CharacterAnimation>().FireAnim();

				// 탄 수 감소
				currentAmmo -= 1;

				// 소음기 장착시 사격 효과음
				if (!silencer)
				{
					shootAudioSource.clip = SoundClips.shootSound;
					shootAudioSource.Play();
				}
				// 소음기 미장착시 사격 효과음
				else
				{
					shootAudioSource.clip = SoundClips.silencerShootSound;
					shootAudioSource.Play();
				}

				if (randomMuzzleflashValue == 1)
				{
					state.SparkParticleTrigger();
					state.MuzzleParticleTrigger();
				}

				// 일반 사격 모드
				if (!isAiming)
				{
					anim.Play("Fire", 0, 0f);

					// 총알의 Rotation을 min 부터 max 값까지 랜덤하게 부여
					Spawnpoints.bulletSpawnPoint.transform.localRotation = Quaternion.Euler(
						Random.Range(normalHitRateMin, normalHitRateMax),
						Random.Range(normalHitRateMin, normalHitRateMax),
						0);
				}
				// 조준 사격 모드
				else
				{
					anim.Play("Aim Fire", 0, 0f);

					// 정확도 100%로 Rotation 조정
					Spawnpoints.bulletSpawnPoint.transform.localRotation = Quaternion.Euler(0, 0, 0);
				}

				// 총알 생성
				var bullet = (Transform)Instantiate (
					Prefabs.bulletPrefab,
					Spawnpoints.bulletSpawnPoint.transform.position,
					Spawnpoints.bulletSpawnPoint.transform.rotation);

				// 총알에 힘 싣기
				bullet.GetComponent<Rigidbody>().velocity = 
					bullet.transform.forward * bulletForce;

				// 탄피 생성
				Instantiate (Prefabs.casingPrefab, 
					Spawnpoints.casingSpawnPoint.transform.position, 
					Spawnpoints.casingSpawnPoint.transform.rotation);

				PlayerHitCheck();
			}
		}

		// 재장전
		if (Input.GetKeyDown(KeyCode.R) && !isReloadingAnim
			&& isCurrentWeapon && !isDraw && !fullAmmo)
			Reload();

		// 걷기
		if (Input.GetKey(KeyCode.W) && !isRunning && !isShooting ||
			Input.GetKey(KeyCode.A) && !isRunning && !isShooting ||
			Input.GetKey(KeyCode.S) && !isRunning && !isShooting ||
			Input.GetKey(KeyCode.D) && !isRunning && !isShooting)
			anim.SetBool("Walk", true);
		else
			anim.SetBool("Walk", false);

		// W와 Left Shift를 누르면 달리기
		if ((Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.LeftShift)))
			isRunning = true;
		else
			isRunning = false;

		// 달리기 애니메이션 설정
		if (isRunning == true)
			anim.SetBool("Run", true);
		else
			anim.SetBool("Run", false);
	}

	private IEnumerator AutoReload()
	{
		yield return null;
		isReloading = true;
		myCharacterModel.GetComponent<CharacterAnimation>().ReloadAnim();

		if (outOfAmmo == true && isCurrentWeapon)
		{
			anim.Play("Reload Out Of Ammo", 0, 0f);

			mainAudioSource.clip = SoundClips.reloadSoundOutOfAmmo;
			mainAudioSource.Play();

			// 장전 중, 떨어진 탄피들을 일정 시간 뒤 인스펙터 창에서 삭제
			if (bulletInMagRenderer != null)
			{
				bulletInMagRenderer.GetComponent
				<SkinnedMeshRenderer>().enabled = false;

				StartCoroutine(ShowBulletInMag());
			}
		}

		// 재장전 완료
		Invoke("SuccessReload", 4.3f);
	}

	// 수동 장전
	private void Reload()
	{
		isReloading = true;
		myCharacterModel.GetComponent<CharacterAnimation>().ReloadAnim();

		anim.Play("Reload Ammo Left", 0, 0f);

		mainAudioSource.clip = SoundClips.reloadSoundAmmoLeft;
		mainAudioSource.Play();

		// 떨어진 탄피들을 인스펙터 창에서 삭제
		if (bulletInMagRenderer != null)
		{
			bulletInMagRenderer.GetComponent
			<SkinnedMeshRenderer>().enabled = true;
		}

		// 재장전 완료
		Invoke("SuccessReload", 3.4f);
	}

	private void SuccessReload()
	{
		currentAmmo = ammo;
		outOfAmmo = false;
		isReloading = false;
	}

	// 탄피는 설정된 시간이 지나면 사라짐
	private IEnumerator ShowBulletInMag()
	{
		yield return new WaitForSeconds(showBulletInMagDelay);
		bulletInMagRenderer.GetComponent<SkinnedMeshRenderer>().enabled = true;
	}

	// 사격 시 총알의 불빛이 사라지는 시간 설정
	private IEnumerator MuzzleFlashLight()
	{
		muzzleFlashLight.enabled = true;
		yield return new WaitForSeconds(lightDuration);
		muzzleFlashLight.enabled = false;

	}
	// 현재 어느 애니메이션이 진행 중인지 확인
	private void AnimationCheck () 
	{
		if (anim.GetCurrentAnimatorStateInfo(0).IsName("Reload Out Of Ammo") ||
			anim.GetCurrentAnimatorStateInfo(0).IsName("Reload Ammo Left"))
			isReloadingAnim = true;
		else
			isReloadingAnim = false;

		if (anim.GetCurrentAnimatorStateInfo(0).IsName("Fire") ||
			anim.GetCurrentAnimatorStateInfo(0).IsName("Aim Fire"))
			isShooting = true;
		else
			isShooting = false;
	}
}