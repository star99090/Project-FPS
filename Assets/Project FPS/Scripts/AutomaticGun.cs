using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Bolt;

public class AutomaticGun : EntityBehaviour<IFPSPlayerState>
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
	private string storedWeaponName;

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
	[Space(10)]

	// 소음기 부착
	public bool silencer;

	[System.Serializable]
	public class weaponAttachmentRenderers
	{
		[Header("Scope Model Renderers")]
		[Space(10)]

		// Renderer
		public SkinnedMeshRenderer scope1Renderer;
		public SkinnedMeshRenderer scope2Renderer;
		public SkinnedMeshRenderer scope3Renderer;
		public SkinnedMeshRenderer scope4Renderer;
		public SkinnedMeshRenderer ironSightsRenderer;
		public SkinnedMeshRenderer silencerRenderer;
		[Header("Scope Sight Mesh Renderers")]
		[Space(10)]

		// Mesh
		public GameObject scope1RenderMesh;
		public GameObject scope2RenderMesh;
		public GameObject scope3RenderMesh;
		public GameObject scope4RenderMesh;
		[Header("Scope Sight Sprite Renderers")]
		[Space(10)]

		// Textures
		public SpriteRenderer scope1SpriteRenderer;
		public SpriteRenderer scope2SpriteRenderer;
		public SpriteRenderer scope3SpriteRenderer;
		public SpriteRenderer scope4SpriteRenderer;
	}
	public weaponAttachmentRenderers WeaponAttachmentRenderers;

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

	private bool isReloadingAnim;
	public bool isReloading = false;
	private bool isRunning;
	private bool isAiming;

	[Tooltip("최대 탄 수")]
	public int ammo;
	private int currentAmmo;
	private bool outOfAmmo;	// 탄을 다 썼는지 확인

	[Header("Bullet Settings")]
	[Tooltip("총탄 발사 힘")]
	public float bulletForce = 0.5f;

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
	public Transform aimSpawnpoint;

	[System.Serializable]
	public class soundClips
	{
		public AudioClip shootSound;
		public AudioClip silencerShootSound;
		public AudioClip takeOtSound;
		public AudioClip reloadSoundOutOfAmmo;
		public AudioClip reloadSoundAmmoLeft;
		public AudioClip aimSound;
	}
	public soundClips SoundClips;

	private bool soundHasPlayed = false;

	[SerializeField] Text attacker;
	public BoltEntity myEntity;
	public GameObject myBody;

	[SerializeField] private GameObject myCharacterModel;
	public bool isCurrentWeapon;

	private void Awake()
	{
		anim = GetComponent<Animator>();
		currentAmmo = ammo;
		muzzleflashLight.enabled = false;

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
			//If scope3 is false, disable scope renderer
			WeaponAttachmentRenderers.scope3Renderer.GetComponent
			<SkinnedMeshRenderer>().enabled = false;
			//Also disable the scope sight render mesh
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
		storedWeaponName = weaponName;
		currentWeaponText.text = weaponName;
		totalAmmoText.text = ammo.ToString();
		initialSwayPosition = transform.localPosition;
		shootAudioSource.clip = SoundClips.shootSound;
	}

    public override void Attached()
    {
		//state.SetAnimator(anim);
		//state.AddCallback("AnimPlay", AnimPlayCallback);
		state.AddCallback("MuzzleParticleTrigger", MuzzleParticleCallback);
		state.AddCallback("SparkParticleTrigger", SparkParticleCallback);
		state.OnMuzzleParticleTrigger += MuzzleParticleCallback;
		state.OnSparkParticleTrigger += SparkParticleCallback;
	}

	void AnimPlayCallback()
    {
		if(state.AnimPlay != "o")
        {
			state.Animator.Play(state.AnimPlay);
			Invoke("AnimPlayDelay", 0.05f);
        }
    }

	void AnimPlayDelay() => state.AnimPlay = "o";

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
		if(hit.collider != null && hit.collider.CompareTag("FPSPlayer"))
        {
			var evnt = PlayerHitEvent.Create();
			evnt.attacker = attacker.text;
			evnt.targetEntity = hit.collider.gameObject.GetComponent<BoltEntity>();
			evnt.damage = Random.Range(30, 35);
			evnt.attackerEntity = myEntity;
			evnt.Send();
		}
    }

	private void LateUpdate()
	{
		if (!entity.IsOwner) return;

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
		if (!entity.IsOwner) return;

		// 우클릭 조준 시 카메라 셋팅
		if (Input.GetButton("Fire2") && !isReloadingAnim && !isRunning & !isReloading)
		{
			if (ironSights == true)
			{
				//state.Aim = true;
				aimFov = ironSightsAimFOV;
				anim.SetBool("Aim", true);//state.Aim);
				//gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView,
				//ironSightsAimFOV, fovSpeed * Time.deltaTime);
			}
			if (scope1 == true)
			{
				//state.AimScope1 = true;
				aimFov = scope1AimFOV;
				anim.SetBool("AimScope1", true);// state.AimScope1);
				WeaponAttachmentRenderers.scope1SpriteRenderer.GetComponent
					<SpriteRenderer>().enabled = true;
				//gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView,
				//scope1AimFOV, fovSpeed * Time.deltaTime);
			}
			if (scope2 == true)
			{
				//state.AimScope2 = true;
				aimFov = scope2AimFOV;
				anim.SetBool("AimScope2", true);//state.AimScope2);
				WeaponAttachmentRenderers.scope2SpriteRenderer.GetComponent
				 <SpriteRenderer>().enabled = true;
				//gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView,
				//scope2AimFOV, fovSpeed * Time.deltaTime);
			}
			if (scope3 == true)
			{
				//state.AimScope3 = true;
				aimFov = scope3AimFOV;
				anim.SetBool("AimScope3", true); //state.AimScope3);
				WeaponAttachmentRenderers.scope3SpriteRenderer.GetComponent
				<SpriteRenderer>().enabled = true;
				//gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView,
				//scope3AimFOV, fovSpeed * Time.deltaTime);
			}
			if (scope4 == true)
			{
				//state.AimScope4 = true;
				aimFov = scope4AimFOV;
				anim.SetBool("AimScope4", true); //state.AimScope4);
				WeaponAttachmentRenderers.scope4SpriteRenderer.GetComponent
				<SpriteRenderer>().enabled = true;
				//gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView,
				//scope4AimFOV, fovSpeed * Time.deltaTime);
			}

			isAiming = true;

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

			//If iron sights are enabled, use normal aim out
			if (ironSights == true)
			{
				//state.Aim = false;
				anim.SetBool("Aim", false); //state.Aim);
			}
			//If scope 1 is enabled, use scope 1 aim out animation
			if (scope1 == true)
			{
				//state.AimScope1 = false;
				anim.SetBool("AimScope1", false); //state.AimScope1);
				WeaponAttachmentRenderers.scope1SpriteRenderer.GetComponent
					<SpriteRenderer>().enabled = false;
			}
			//If scope 2 is enabled, use scope 2 aim out animation
			if (scope2 == true)
			{
				//state.AimScope2 = false;
				anim.SetBool("AimScope2", false); //state.AimScope2);
				WeaponAttachmentRenderers.scope2SpriteRenderer.GetComponent
				<SpriteRenderer>().enabled = false;
			}
			//If scope 3 is enabled, use scope 3 aim out animation
			if (scope3 == true)
			{
				//state.AimScope3 = false;
				anim.SetBool("AimScope3", false); //state.AimScope3);
				WeaponAttachmentRenderers.scope3SpriteRenderer.GetComponent
				<SpriteRenderer>().enabled = false;
			}
			//If scope 4 is enabled, use scope 4 aim out animation
			if (scope4 == true)
			{
				//state.AimScope4 = false;
				anim.SetBool("AimScope4", false); //state.AimScope4);
				WeaponAttachmentRenderers.scope4SpriteRenderer.GetComponent
				<SpriteRenderer>().enabled = false;
			}

			soundHasPlayed = false;
		}
		
		// 발사 이펙트 빈도 수 지정
		randomMuzzleflashValue = Random.Range(1, 2);

		// 현재 탄 수 동기화
		currentAmmoText.text = currentAmmo.ToString();

		// 현재 재장전 애니메이션 진행 중인지 확인
		AnimationCheck();

		// 탄 다 썼을 때
		if (currentAmmo == 0 && isCurrentWeapon)
		{
			currentWeaponText.text = "OUT OF AMMO";

			outOfAmmo = true;
			if (autoReload == true && !isReloadingAnim)
				StartCoroutine(AutoReload());
		}
		else
		{
			currentWeaponText.text = storedWeaponName.ToString();
			
			outOfAmmo = false;
		}

		// 자동 발사(좌클릭 유지)
		if (Input.GetMouseButton(0) && !outOfAmmo && !isReloadingAnim && !isRunning && !isReloading && isCurrentWeapon)
		{
			if (Time.time - lastFired > 1 / fireRate)
			{
				lastFired = Time.time;

				myCharacterModel.GetComponent<CharacterAnimation>().FireAnim();
				// 탄 수 감소
				currentAmmo -= 1;

				// 소음기 장착시 사격 효과음
				if (silencer == true && WeaponAttachmentRenderers.silencerRenderer != null)
				{
					shootAudioSource.clip = SoundClips.silencerShootSound;
					shootAudioSource.Play();
				}
				// 소음기 미장착시 사격 효과음
				else
				{
					shootAudioSource.clip = SoundClips.shootSound;
					shootAudioSource.Play();
				}

				if (randomMuzzleflashValue == 1 && !silencer)
				{
					state.SparkParticleTrigger();
					state.MuzzleParticleTrigger();
				}

				myBody.GetComponent<CharacterAnimation>().FireAnim();
				// 일반 사격 모드
				if (!isAiming)
				{
					//state.AnimPlay = "Fire";
					anim.Play("Fire", 0, 0f);

					// 총알 생성
					var bullet = Instantiate(
						Prefabs.bulletPrefab,
						Spawnpoints.bulletSpawnPoint.transform.position,
						Spawnpoints.bulletSpawnPoint.transform.rotation);

					// 총알에 힘 싣기
					bullet.GetComponent<Rigidbody>().velocity =
						bullet.transform.forward * bulletForce;
				}
				// 조준 사격 모드
				else
				{
					if (ironSights == true)
					{
						anim.Play("Aim Fire", 0, 0f);
						//state.AnimPlay = "Aim Fire";
					}
					if (scope1 == true)
						anim.Play("Aim Fire Scope 1", 0, 0f);
						//state.AnimPlay = "Aim Fire Scope 1";
					if (scope2 == true)
						anim.Play("Aim Fire Scope 2", 0, 0f);
						//state.AnimPlay = "Aim Fire Scope 2";
					if (scope3 == true)
						anim.Play("Aim Fire Scope 3", 0, 0f);
					//state.AnimPlay = "Aim Fire Scope 3";
					if (scope4 == true)
						anim.Play("Aim Fire Scope 4", 0, 0f);
					//state.AnimPlay = "Aim Fire Scope 4";

					// 총알 생성
					var bullet = Instantiate(
						Prefabs.bulletPrefab,
						aimSpawnpoint.position,
						Spawnpoints.bulletSpawnPoint.transform.rotation);

					// 총알에 힘 싣기
					bullet.GetComponent<Rigidbody>().velocity =
						bullet.transform.forward * bulletForce;
				}

				// 탄피 생성
				Instantiate(Prefabs.casingPrefab,
					Spawnpoints.casingSpawnPoint.transform.position,
					Spawnpoints.casingSpawnPoint.transform.rotation);

				PlayerHitCheck();
			}
		}

		// 재장전
		if (Input.GetKeyDown(KeyCode.R) && !isReloadingAnim && isCurrentWeapon)
			Reload();

		// 걷기
		if (Input.GetKey(KeyCode.W) && !isRunning ||
			Input.GetKey(KeyCode.A) && !isRunning ||
			Input.GetKey(KeyCode.S) && !isRunning ||
			Input.GetKey(KeyCode.D) && !isRunning)
		{
			//myBody.GetComponent<CharacterAnimation>().move = true;
			anim.SetBool("Walk", true); //state.Walk);
		}
		else
		{
			//myBody.GetComponent<CharacterAnimation>().move = false;
			anim.SetBool("Walk", false); //state.Walk);
		}

		// W와 Left Shift를 누르면 달리기
		if ((Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.LeftShift)))
			isRunning = true;
		else
			isRunning = false;

		// 달리기 애니메이션 설정
		if (isRunning == true)
		{
			//state.Run = true;
			anim.SetBool("Run", true); //state.Run);
		}
		else
		{
			//state.Run = false;
			anim.SetBool("Run", false); //state.Run);
		}
	}

	// 자동 장전
	private IEnumerator AutoReload()
	{
		isReloading = true;
		myBody.GetComponent<CharacterAnimation>().ReloadAnim();
		yield return new WaitForSeconds(autoReloadDelay);

		if (outOfAmmo == true && isCurrentWeapon)
		{
			anim.Play("Reload Out Of Ammo", 0, 0f);
			//state.AnimPlay = "Reload Out Of Ammo";

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

		myCharacterModel.GetComponent<CharacterAnimation>().ReloadAnim();

		// 재장전 완료
		Invoke("SuccessReload", 1.5f);
	}

	// 수동 장전
	private void Reload()
	{
		isReloading = true;
		myBody.GetComponent<CharacterAnimation>().ReloadAnim();
		if (outOfAmmo == true)
		{
			anim.Play("Reload Out Of Ammo", 0, 0f);
			//state.AnimPlay = "Reload Out Of Ammo";

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
			//state.AnimPlay = "Reload Ammo Left";

			mainAudioSource.clip = SoundClips.reloadSoundAmmoLeft;
			mainAudioSource.Play();

			// 장전 중이 아닐 때, 떨어진 탄피들을 인스펙터 창에서 삭제
			if (bulletInMagRenderer != null)
			{
				bulletInMagRenderer.GetComponent
				<SkinnedMeshRenderer>().enabled = true;
			}
		}

		myCharacterModel.GetComponent<CharacterAnimation>().ReloadAnim();

		// 재장전 완료
		Invoke("SuccessReload", 1.5f);
	}

	void SuccessReload()
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
		muzzleflashLight.enabled = true;
		yield return new WaitForSeconds(lightDuration);
		muzzleflashLight.enabled = false;
	}

	// 현재 재장전 애니메이션 진행 중인지 확인
	private void AnimationCheck()
	{
		if (anim.GetCurrentAnimatorStateInfo(0).IsName("Reload Out Of Ammo") ||
			anim.GetCurrentAnimatorStateInfo(0).IsName("Reload Ammo Left"))
			isReloadingAnim = true;
		else
			isReloadingAnim = false;
	}
}