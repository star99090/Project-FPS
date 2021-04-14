using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Bolt;

public class AutomaticGunScriptLPFP : EntityBehaviour<IFPSPlayerState>
{
	Animator anim;

	[Header("Gun Camera")]
	public Camera gunCamera;

	[Header("Gun Camera Options")]
	[Tooltip("조준 시 카메라 변경 속도")]
	public float fovSpeed = 15.0f;

	[Tooltip("카메라 시야 기본 값")]
	public float defaultFov = 40.0f;

	public float aimFov = 25.0f;

	[Header("Weapon Name UI")]
	[Tooltip("총기 이름")]
	public string weaponName;
	private string storedWeaponName;

	[Header("Weapon Sway")]
	[Tooltip("총기 흔듦")]
	public bool weaponSway;

	public float swayAmount = 0.02f;
	public float maxSwayAmount = 0.06f;
	public float swaySmoothValue = 4.0f;

	private Vector3 initialSwayPosition;

	[Header("Weapon Settings")]
	[Tooltip("발사 속도")]
	public float fireRate;

	[Tooltip("자동 장전")]
	public bool autoReload;

	[Tooltip("발사 딜레이")]
	public float autoReloadDelay;
	private float lastFired;

	private bool isReloading;
	private bool isRunning;
	private bool isAiming;
	private bool isWalking;

	[Tooltip("최대 탄 수")]
	public int ammo;
	private int currentAmmo;
	private bool outOfAmmo;	// 탄을 다 썼는지 확인

	[Header("Bullet Settings")]
	[Tooltip("총탄 발사 힘")]
	public float bulletForce = 500.0f;

	[Tooltip("탄피 자동 삭제에 걸리는 딜레이")]
	public float showBulletInMagDelay = 0.6f;
	[Tooltip("탄피 안의 총알 모델")]
	public SkinnedMeshRenderer bulletInMagRenderer;

	private int randomMuzzleflashValue;

	public ParticleSystem muzzleParticles;
	public ParticleSystem sparkParticles;
	public int minSparkEmission = 1;
	public int maxSparkEmission = 7;

	[Header("Muzzleflash Light Settings")]
	public Light muzzleflashLight;
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
		public AudioClip takeOtSound;
		public AudioClip reloadSoundOutOfAmmo;
		public AudioClip reloadSoundAmmoLeft;
		public AudioClip aimSound;
	}
	public soundClips SoundClips;

	private bool soundHasPlayed = false;

	private void Awake()
	{
		anim = GetComponent<Animator>();
		currentAmmo = ammo;
		muzzleflashLight.enabled = false;
	}

    private void Start()
	{
		storedWeaponName = weaponName;
		currentWeaponText.text = weaponName;
		totalAmmoText.text = ammo.ToString();
		initialSwayPosition = transform.localPosition;
		shootAudioSource.clip = SoundClips.shootSound;
	}

	private void LateUpdate()
	{
		//if (!entity.IsOwner) return;

		// 무기 흔들면서 드는 것
		if (weaponSway == true)
		{
			// 마우스 현재 회전 반영
			float movementX = -Input.GetAxis("Mouse X") * swayAmount;
			float movementY = -Input.GetAxis("Mouse Y") * swayAmount;
			
			movementX = Mathf.Clamp
				(movementX, -maxSwayAmount, maxSwayAmount);
			movementY = Mathf.Clamp
				(movementY, -maxSwayAmount, maxSwayAmount);
			
			Vector3 finalSwayPosition = new Vector3(movementX, movementY, 0);

			// 흔들고 정상 위치로 포지션 복귀를 위한 Lerp
			transform.localPosition = Vector3.Lerp(transform.localPosition,
				finalSwayPosition + initialSwayPosition, Time.deltaTime * swaySmoothValue);
		}
	}

    private void Update()
	{
		//if (!entity.IsOwner) return;

		// 우클릭 조준 시 카메라 셋팅
		if (Input.GetButton("Fire2") && !isReloading && !isRunning)
		{
			isAiming = true;
			anim.SetBool("Aim", true);

			gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView,
				aimFov, fovSpeed * Time.deltaTime);

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

			isAiming = false;
			anim.SetBool("Aim", false);

			soundHasPlayed = false;
		}
		
		// 발사 이펙트 빈도 수 지정
		randomMuzzleflashValue = Random.Range(1, 2);

		// 현재 탄 수 동기화
		currentAmmoText.text = currentAmmo.ToString();

		// 현재 재장전 애니메이션 진행 중인지 확인
		AnimationCheck();

		// 탄 다 썼을 때
		if (currentAmmo == 0)
		{
			currentWeaponText.text = "OUT OF AMMO";

			outOfAmmo = true;
			if (autoReload == true && !isReloading)
				StartCoroutine(AutoReload());
		}
		else
		{
			currentWeaponText.text = storedWeaponName.ToString();
			
			outOfAmmo = false;
			//anim.SetBool ("Out Of Ammo", false);
		}

		// 자동 발사(좌클릭 유지)
		if (Input.GetMouseButton(0) && !outOfAmmo && !isReloading && !isRunning)
		{
			if (Time.time - lastFired > 1 / fireRate)
			{
				lastFired = Time.time;

				// 탄 수 감소
				currentAmmo -= 1;

				shootAudioSource.clip = SoundClips.shootSound;
				shootAudioSource.Play();

				// 일반 사격 모드
				if (!isAiming)
				{
					anim.Play("Fire", 0, 0f);

					if (randomMuzzleflashValue == 1)
					{
						sparkParticles.Emit(Random.Range(minSparkEmission, maxSparkEmission));
						muzzleParticles.Emit(1);
						
						StartCoroutine(MuzzleFlashLight());
					}
				}
				// 조준 사격 모드
				else
				{
					anim.Play("Aim Fire", 0, 0f);
					if (randomMuzzleflashValue == 1)
					{
						sparkParticles.Emit(Random.Range(minSparkEmission, maxSparkEmission));
						muzzleParticles.Emit(1);

						StartCoroutine(MuzzleFlashLight());
					}
				}

				// 총알 생성
				var bullet = (Transform)Instantiate(
					Prefabs.bulletPrefab,
					Spawnpoints.bulletSpawnPoint.transform.position,
					Spawnpoints.bulletSpawnPoint.transform.rotation);

				// 총알에 힘 싣기
				bullet.GetComponent<Rigidbody>().velocity =
					bullet.transform.forward * bulletForce;

				// 탄피 생성
				Instantiate(Prefabs.casingPrefab,
					Spawnpoints.casingSpawnPoint.transform.position,
					Spawnpoints.casingSpawnPoint.transform.rotation);
			}
		}

		// 재장전
		if (Input.GetKeyDown(KeyCode.R) && !isReloading)
			Reload();

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

	// 자동 장전
	private IEnumerator AutoReload()
	{
		yield return new WaitForSeconds(autoReloadDelay);

		if (outOfAmmo == true)
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
		currentAmmo = ammo;
		outOfAmmo = false;
	}

	// 수동 장전
	private void Reload()
	{

		if (outOfAmmo == true)
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
		else
		{
			anim.Play("Reload Ammo Left", 0, 0f);

			mainAudioSource.clip = SoundClips.reloadSoundAmmoLeft;
			mainAudioSource.Play();

			// 장전 중이 아닐 때, 떨어진 탄피들을 인스펙터 창에서 삭제
			if (bulletInMagRenderer != null)
			{
				bulletInMagRenderer.GetComponent
				<SkinnedMeshRenderer>().enabled = true;
			}
		}

		// 재장전 완료
		currentAmmo = ammo;
		outOfAmmo = false;
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

		muzzleflashLight.enabled = true;
		yield return new WaitForSeconds(lightDuration);
		muzzleflashLight.enabled = false;
	}

	// 현재 재장전 애니메이션 진행 중인지 확인
	private void AnimationCheck()
	{
		if (anim.GetCurrentAnimatorStateInfo(0).IsName("Reload Out Of Ammo") ||
			anim.GetCurrentAnimatorStateInfo(0).IsName("Reload Ammo Left"))
			isReloading = true;
		else
			isReloading = false;
	}
}