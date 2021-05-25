using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Bolt;

public class RocketLauncherScriptLPFP : EntityBehaviour<IFPSPlayerState>
{
	public Animator anim;

	[Header("Gun Camera")]
	public Camera gunCamera;

	[Header("Gun Camera Options")]
	[Tooltip("조준 시 카메라 변경 속도")]
	public float fovSpeed = 15.0f;

	[Tooltip("카메라 시야 기본 값")]
	public float defaultFov = 40.0f;

	public float aimFov = 18.0f;

	[Header("Weapon Name UI")]
	[Tooltip("총기 이름")]
	public string weaponName;

	[Header("Rocket Launcher Projectile")]
	[Space(10)]
	public SkinnedMeshRenderer projectileRenderer;

	[Header("Hit Rate Settings")]
	[Tooltip("일반 사격 명중률 조정")]
	[Range(-5, 5)]
	public float normalHitRateMin = -5f;
	[Range(-5, 5)]
	public float normalHitRateMax = 5f;
	[Space(10)]
	[Tooltip("조준 사격 명중률 조정")]
	[Range(-1, 1)]
	public float aimHitRateMin = -1f;
	[Range(-1, 1)]
	public float aimHitRateMax = 1f;

	[Header("Weapon Settings")]
	private float showProjectileDelay;

	[Tooltip("데미지 중간 값")]
	public int damage = 75;

	private int ammo = 1;
	private int currentAmmo;
	private bool outOfAmmo;

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
		public Transform projectilePrefab;
	}
	public prefabs Prefabs;
	
	[System.Serializable]
	public class spawnpoints
	{  
		[Header("Spawnpoints")]
		public Transform bulletSpawnPoint;
	}
	public spawnpoints Spawnpoints;


	[System.Serializable]
	public class soundClips
	{
		public AudioClip shootSound;
		public AudioClip takeOutSound;
		public AudioClip reloadSound;
		public AudioClip aimSound;
	}
	public soundClips SoundClips;

	private bool soundHasPlayed = false;

	[Header("Other Settings")]
	[SerializeField] private BoltEntity myEntity;
	[SerializeField] private GameObject myCharacterModel;
	[SerializeField] private Text attacker;
	public bool isCurrentWeapon;

	private bool isDraw = true;
	private bool isReloadingAnim;
	private bool isReloading;
	private bool isRunning;
	private bool isAiming;

	private void Awake ()
	{
		anim = GetComponent<Animator>();
		currentAmmo = ammo;
		muzzleFlashLight.enabled = false;
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

	void PlayerHitCheck()
	{
		Physics.Raycast(Spawnpoints.bulletSpawnPoint.position, Spawnpoints.bulletSpawnPoint.forward, out RaycastHit hit);
		if (hit.collider != null && hit.collider.gameObject.CompareTag("FPSPlayer"))
		{
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

		if (isDraw && !anim.GetCurrentAnimatorStateInfo(0).IsName("Draw"))
			isDraw = false;

		// 우클릭 조준 시 카메라 셋팅
		if (Input.GetButton("Fire2") && !isReloadingAnim && !isRunning && !isReloading && !isDraw)
		{
			isAiming = true;
			
			gunCamera.fieldOfView = Mathf.Lerp (gunCamera.fieldOfView,
			aimFov, fovSpeed * Time.deltaTime);

			anim.SetBool ("Aim", true);

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

			anim.SetBool ("Aim", false);

			soundHasPlayed = false;
		}

		// 발사 이펙트 빈도 수 지정
		randomMuzzleflashValue = Random.Range(1, 2);

		// 현재 탄 수 동기화
		currentAmmoText.text = currentAmmo.ToString();

		// 현재 재장전 애니메이션 진행 중인지 확인
		AnimationCheck();

		if (currentAmmo == 0 && isCurrentWeapon) 
		{
			currentWeaponText.text = "OUT OF AMMO";

			outOfAmmo = true;
			if (!isReloadingAnim)
			{
				StartCoroutine(ShowProjectileDelay());
				AutoReload();
			}
		}
		else
		{
			currentWeaponText.text = weaponName;

			outOfAmmo = false;
		}

		// 발사 
		if (Input.GetMouseButtonDown(0) && !outOfAmmo && !isReloadingAnim
			&& !isRunning && !isReloading && isCurrentWeapon && !isDraw)
		{
			myCharacterModel.GetComponent<CharacterAnimation>().FireAnim();

			// 탄 수 감소
			currentAmmo -= 1;

			shootAudioSource.clip = SoundClips.shootSound;
			shootAudioSource.Play ();

			state.SparkParticleTrigger();
			state.MuzzleParticleTrigger();

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

				// 총알의 Rotation을 min 부터 max 값까지 랜덤하게 부여
				Spawnpoints.bulletSpawnPoint.transform.localRotation = Quaternion.Euler(
					Random.Range(aimHitRateMin, aimHitRateMax),
					Random.Range(aimHitRateMin, aimHitRateMax),
					0);
			}
			
			// 탄두 생성
			Instantiate(
				Prefabs.projectilePrefab,
				Spawnpoints.bulletSpawnPoint.transform.position,
				Spawnpoints.bulletSpawnPoint.transform.rotation);
		}

		// 걷기
		if (Input.GetKey(KeyCode.W) && !isRunning ||
			Input.GetKey(KeyCode.A) && !isRunning ||
			Input.GetKey(KeyCode.S) && !isRunning ||
			Input.GetKey(KeyCode.D) && !isRunning)
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

	private IEnumerator ShowProjectileDelay()
	{
		projectileRenderer.GetComponent<SkinnedMeshRenderer> ().enabled = false;
		yield return new WaitForSeconds (showProjectileDelay);
		projectileRenderer.GetComponent<SkinnedMeshRenderer>().enabled = true;
	}

	private void AutoReload()
	{
		if (outOfAmmo == true) 
		{
			anim.Play ("Reload", 0, 0f);

			mainAudioSource.clip = SoundClips.reloadSound;
			mainAudioSource.Play ();
		}

		// 재장전 완료
		Invoke("SuccessReload", 2.1f);
	}

	void SuccessReload()
	{
		currentAmmo = ammo;
		outOfAmmo = false;
		isReloading = false;
	}

	// 사격 시 총알의 불빛이 사라지는 시간 설정
	private IEnumerator MuzzleFlashLight()
	{
		muzzleFlashLight.enabled = true;
		yield return new WaitForSeconds(lightDuration);
		muzzleFlashLight.enabled = false;
	}

	// 현재 재장전 애니메이션 진행 중인지 확인
	private void AnimationCheck()
	{
		if (anim.GetCurrentAnimatorStateInfo(0).IsName("Reload"))
			isReloadingAnim = true;
		else
			isReloadingAnim = false;
	}
}