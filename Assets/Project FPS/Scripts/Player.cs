﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static NetworkManager;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(AudioSource))]
public class Player : Bolt.EntityBehaviour<IFPSPlayerState>
{
#pragma warning disable 649
    [Header("Arms")]
    [Tooltip("카메라의 위치"), SerializeField]
    private Transform arms;

    [Tooltip("캐릭터에 상대적인 팔과 카메라의 위치"), SerializeField]
    private Vector3 armPosition;
    private Vector3 armSubPos = new Vector3(0f, 0.2f, 0f);

    [Header("Audio Clips")]
    [Tooltip("걷는 효과음"), SerializeField]
    private AudioClip walkingSound;

    [Tooltip("달리는 효과음"), SerializeField]
    private AudioClip runningSound;

    [Header("Movement Settings")]
    [Tooltip("걷기 속도"), SerializeField]
    private float walkingSpeed = 5f;

    [Tooltip("달리기 속도"), SerializeField]
    private float runningSpeed = 9f;

    [Tooltip("최대 이동속도에 걸리는 시간"), SerializeField]
    private float movementSmoothness = 0.125f;

    [Tooltip("점프력"), SerializeField]
    private float jumpForce = 70f;

    [Header("Look Settings")]
    [Tooltip("마우스 회전 감도"), SerializeField]
    public float mouseSensitivity;

    [Tooltip("최대 회전 속도에 걸리는 시간"), SerializeField]
    private float rotationSmoothness = 0.05f;

    [Tooltip("팔과 카메라의 최소 회전각"), SerializeField]
    private float minVerticalAngle = -90f;

    [Tooltip("팔과 카메라의 최대 회전각"), SerializeField]
    private float maxVerticalAngle = 90f;

    [Tooltip("조작법"), SerializeField]
    private FpsInput input;

    [Header("Weapon Settings")]
    [Tooltip("사용할 무기")]
    public GameObject[] myWeapon;
    public Sprite[] myWeaponIcon;
    public bool isWeaponChange = false;
#pragma warning restore 649

    [Header("Other Settings")]
    public Text nicknameText;
    [SerializeField] Transform nicknameCanvas;
    [SerializeField] Image gunIcon;
    [SerializeField] GameObject progressPannel;
    [SerializeField] Text playersScore;

    private Rigidbody _rigidbody;
    private CapsuleCollider _collider;
    private AudioSource _audioSource;
    private SmoothRotation _rotationX;
    private SmoothRotation _rotationY;
    private SmoothVelocity _velocityX;
    private SmoothVelocity _velocityZ;
    private float Volume = 0.8f;
    private bool _isGrounded;
    private bool isTab = false;
    public bool isESC = false;
    public bool isSettings = false;
    public GameObject ESCPanel;
    public GameObject SettingsPanel;
    public Slider SensitivitySlider;
    public Slider VolumeSlider;
    public Toggle VolumeToggle;

    private readonly RaycastHit[] _groundCastResults = new RaycastHit[8];
    private readonly RaycastHit[] _wallCastResults = new RaycastHit[8];

    private void Start()
    {
        myWeapon[0].GetComponent<HandgunScriptLPFP>().isCurrentWeapon = true;
        gunIcon.sprite = myWeaponIcon[0];
        if (entity.IsOwner) NM.myPlayer = this.entity;

        state.nickname = TitleLobbyManager.TLM.myNickname;
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        _collider = GetComponent<CapsuleCollider>();
        _audioSource = GetComponent<AudioSource>();
        arms = AssignCharactersCamera();
        _audioSource.clip = walkingSound;
        _audioSource.loop = true;
        _rotationX = new SmoothRotation(RotationXRaw);
        _rotationY = new SmoothRotation(RotationYRaw);
        _velocityX = new SmoothVelocity();
        _velocityZ = new SmoothVelocity();
        Cursor.lockState = CursorLockMode.Locked;
        ValidateRotationRestriction();
    }

    public override void Attached()
    {
        state.AddCallback("nickname", NicknameCallback);
    }

    void NicknameCallback() => nicknameText.text = state.nickname;

    // 팔이 캐릭터의 회전과 위치를 따라가도록 구현
    private Transform AssignCharactersCamera()
    {
        var t = transform;
        arms.SetPositionAndRotation(t.position, t.rotation);
        return arms;
    }

    // 회전각 max의 값보다 min의 값이 큰 경우를 방지
    private void ValidateRotationRestriction()
    {
        minVerticalAngle = ClampRotationRestriction(minVerticalAngle, -90, 90);
        maxVerticalAngle = ClampRotationRestriction(maxVerticalAngle, -90, 90);
        if (maxVerticalAngle >= minVerticalAngle) return;
        Debug.LogWarning("maxVerticalAngle should be greater than minVerticalAngle.");
        var min = minVerticalAngle;
        minVerticalAngle = maxVerticalAngle;
        maxVerticalAngle = min;
    }

    // 입력된 값이 min, max 값 내의 값인지 확인하여 그 사이의 값으로 반환
    private static float ClampRotationRestriction(float rotationRestriction, float min, float max)
    {
        if (rotationRestriction >= min && rotationRestriction <= max) return rotationRestriction;
        var message = string.Format("Rotation restrictions should be between {0} and {1} degrees.", min, max);
        Debug.LogWarning(message);
        return Mathf.Clamp(rotationRestriction, min, max);
    }

    // 캐릭터가 지면에 붙어 있는지 검사
    private void OnCollisionStay()
    {
        // 콜라이더의 경계를 가져온다
        var bounds = _collider.bounds;

        // 경계의 중심(콜라이더의 중심)
        var extents = bounds.extents;

        // 반지름보다 0.01f 작은 값 저장
        var radius = extents.x - 0.01f;

        // 캐릭터 내의 모든 콜라이더에 부딪히는 여러 물체에 대한 정보를 RaycastHit[] 배열에 저장
        Physics.SphereCastNonAlloc(bounds.center, radius, Vector3.down,
            _groundCastResults, extents.y - radius * 0.5f, ~0, QueryTriggerInteraction.Ignore);

        // 부딪힌 물체가 없거나 캐릭터의 콜라이더라면 무시
        if (!_groundCastResults.Any(hit => hit.collider != null && hit.collider != _collider)) return;

        // 부딪힌 물체를 RaycastHit 배열에 저장
        for (var i = 0; i < _groundCastResults.Length; i++)
            _groundCastResults[i] = new RaycastHit();

        // 점프를 하고 착지하면 지면과 부딪히기때문에 _isGrounded를 true로 설정
        _isGrounded = true;
    }

    // 캐릭터와 카메라의 이동과 회전을 처리
    private void FixedUpdate()
    {
        if (!NM.isResult)
        {
            if (!entity.IsOwner || isESC || isSettings) return;

            RotateCameraAndCharacter();
            MoveCharacter();
            _isGrounded = false;
        }
        else
            Cursor.lockState = CursorLockMode.None;
    }

    private void ProgressScore()
    {
        playersScore.text = "";

        List<string> nickList = new List<string>();
        List<int> scoreList = new List<int>();

        int scoreTemp = 0;
        string nickTemp = "";

        for (int i = 0; i < NM.players.Count; i++)
        {
            nickList.Add(NM.players[i].GetComponent<Player>().nicknameText.text);
            scoreList.Add(NM.players[i].GetComponent<PlayerSubScript>().myKillScore);
        }
        
        // 닉네임과 점수 정렬
        for (int i = 0; i < scoreList.Count - 1; i++)
        {
            for (int j = i + 1; j < scoreList.Count; j++)
            {
                if (scoreList[i] < scoreList[j])
                {
                    scoreTemp = scoreList[i];
                    scoreList[i] = scoreList[j];
                    scoreList[j] = scoreTemp;

                    nickTemp = nickList[i];
                    nickList[i] = nickList[j];
                    nickList[j] = nickTemp;
                }
            }
        }

        for (int i = 0; i < nickList.Count; i++)
            playersScore.text += nickList[i] + " : " + scoreList[i] + "Kill\n";
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isSettings)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    isESC = true;
                    Cursor.lockState = CursorLockMode.None;
                    ESCPanel.SetActive(true);
                }
                else
                {
                    isESC = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    ESCPanel.SetActive(false);
                }
            }
            else
            {
                isESC = false;
                isSettings = false;
                Cursor.lockState = CursorLockMode.Locked;
                SettingsPanel.SetActive(false);
            }
        }

        PlayFootstepSounds();
        if (!entity.IsOwner || NM.isResult || isESC || isSettings) return;

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            mouseSensitivity += 0.5f;
            if (mouseSensitivity > 15f)
                mouseSensitivity = 15f;
            SensitivitySlider.value = mouseSensitivity;
        }

        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            mouseSensitivity -= 0.5f;
            if (mouseSensitivity < 0.5f)
                mouseSensitivity = 0.5f;
            SensitivitySlider.value = mouseSensitivity;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isTab = !isTab;

            if (isTab)
            {
                ProgressScore();
                progressPannel.SetActive(true);
            }
            else
                progressPannel.SetActive(false);

            GetComponent<PlayerSubScript>().ProgressSub(!isTab);
        }

        if (isWeaponChange)
        {
            int mKS = GetComponent<PlayerSubScript>().myKillScore;

            if (mKS < myWeapon.Length)
            {
                if (myWeapon[mKS - 1].CompareTag("AutomaticGun"))
                    myWeapon[mKS - 1].GetComponent<AutomaticGun>().isCurrentWeapon = false;
                else if (myWeapon[mKS - 1].CompareTag("GrenadeLauncher"))
                    myWeapon[mKS - 1].GetComponent<GrenadeLauncherScriptLPFP>().isCurrentWeapon = false;
                else if (myWeapon[mKS - 1].CompareTag("Shotgun"))
                    myWeapon[mKS - 1].GetComponent<PumpShotgunScriptLPFP>().isCurrentWeapon = false;
                else if (myWeapon[mKS - 1].CompareTag("Handgun"))
                    myWeapon[mKS - 1].GetComponent<HandgunScriptLPFP>().isCurrentWeapon = false;
                else if(myWeapon[mKS - 1].CompareTag("Sniper"))
                    myWeapon[mKS - 1].GetComponent<SniperScriptLPFP>().isCurrentWeapon = false;
                else if (myWeapon[mKS - 1].CompareTag("BoltActionSniper"))
                    myWeapon[mKS - 1].GetComponent<BoltActionSniperScriptLPFP>().isCurrentWeapon = false;
                else if (myWeapon[mKS - 1].CompareTag("RocketLauncher"))
                    myWeapon[mKS - 1].GetComponent<RocketLauncherScriptLPFP>().isCurrentWeapon = false;
                
                myWeapon[mKS - 1].SetActive(false);

                myWeapon[mKS].SetActive(true);
                gunIcon.sprite = myWeaponIcon[mKS];
                if (myWeapon[mKS].CompareTag("AutomaticGun"))
                    myWeapon[mKS].GetComponent<AutomaticGun>().isCurrentWeapon = true;
                else if (myWeapon[mKS].CompareTag("GrenadeLauncher"))
                    myWeapon[mKS].GetComponent<GrenadeLauncherScriptLPFP>().isCurrentWeapon = true;
                else if (myWeapon[mKS].CompareTag("Shotgun"))
                    myWeapon[mKS].GetComponent<PumpShotgunScriptLPFP>().isCurrentWeapon = true;
                else if (myWeapon[mKS].CompareTag("Handgun"))
                    myWeapon[mKS].GetComponent<HandgunScriptLPFP>().isCurrentWeapon = true;
                else if(myWeapon[mKS].CompareTag("Sniper"))
                    myWeapon[mKS].GetComponent<SniperScriptLPFP>().isCurrentWeapon = true;
                else if (myWeapon[mKS].CompareTag("BoltActionSniper"))
                    myWeapon[mKS].GetComponent<BoltActionSniperScriptLPFP>().isCurrentWeapon = true;
                else if (myWeapon[mKS].CompareTag("RocketLauncher"))
                    myWeapon[mKS].GetComponent<RocketLauncherScriptLPFP>().isCurrentWeapon = true;
            }
            isWeaponChange = false;
        }

        arms.position = transform.position + transform.TransformVector(armPosition) + armSubPos;
        Jump();
    }

    void LateUpdate() => nicknameCanvas.LookAt(Camera.main.transform);

    // 카메라와 캐릭터가 보는 방향에 대한 회전
    private void RotateCameraAndCharacter()
    {
        var rotationX = _rotationX.Update(RotationXRaw, rotationSmoothness);
        var rotationY = _rotationY.Update(RotationYRaw, rotationSmoothness);
        var clampedY = RestrictVerticalRotation(rotationY);
        _rotationY.Current = clampedY;
        var worldUp = arms.InverseTransformDirection(Vector3.up);
        var rotation = arms.rotation *
                       Quaternion.AngleAxis(rotationX, worldUp) *
                       Quaternion.AngleAxis(clampedY, Vector3.left);
        transform.eulerAngles = new Vector3(0f, rotation.eulerAngles.y, 0f);
        arms.rotation = rotation;
    }

    // y축으로 보정이 없는 카메라 초점의 움직임을 보정하여 반환
    private float RotationXRaw
    {
        get { return input.RotateX * mouseSensitivity; }
    }

    // x축으로 보정이 없는 카메라 초점의 움직임을 보정하여 반환
    private float RotationYRaw
    {
        get { return input.RotateY * mouseSensitivity; }
    }

    // 카메라의 x축 각도를 최소~최대 회전각 사이로 고정
    private float RestrictVerticalRotation(float mouseY)
    {
        var currentAngle = NormalizeAngle(arms.eulerAngles.x);
        var minY = minVerticalAngle + currentAngle;
        var maxY = maxVerticalAngle + currentAngle;
        return Mathf.Clamp(mouseY, minY + 0.01f, maxY - 0.01f);
    }

    // 회전 각도가 -180 ~ 180도를 유지하도록 유도
    private static float NormalizeAngle(float angleDegrees)
    {
        while (angleDegrees > 180f)
            angleDegrees -= 360f;

        while (angleDegrees <= -180f)
            angleDegrees += 360f;

        return angleDegrees;
    }

    // 캐릭터 이동
    private void MoveCharacter()
    {
        var direction = new Vector3(input.Move, 0f, input.LeftRight).normalized;
        var worldDirection = transform.TransformDirection(direction);
        var velocity = worldDirection * (input.Run ? runningSpeed : walkingSpeed);

        // 벽이나 오브젝트에 부딪힐 때 캐릭터가 버벅거리거나 멈추지 않도록 충돌을 미리 확인
        var intersectsWall = CheckCollisionsWithWalls(velocity);
        if (intersectsWall)
        {
            _velocityX.Current = _velocityZ.Current = 0f;
            return;
        }

        var smoothX = _velocityX.Update(velocity.x, movementSmoothness);
        var smoothZ = _velocityZ.Update(velocity.z, movementSmoothness);
        var rigidbodyVelocity = _rigidbody.velocity;
        var force = new Vector3(smoothX - rigidbodyVelocity.x, 0f, smoothZ - rigidbodyVelocity.z);
        _rigidbody.AddForce(force, ForceMode.VelocityChange);
    }

    // 벽과 붙어있는지 검사
    private bool CheckCollisionsWithWalls(Vector3 velocity)
    {
        // 지면은 제외
        if (_isGrounded) return false;

        var bounds = _collider.bounds;
        var radius = _collider.radius;
        var halfHeight = _collider.height * 0.5f - radius * 1.0f;
        var point1 = bounds.center;
        point1.y += halfHeight;
        var point2 = bounds.center;
        point2.y -= halfHeight;

        // CapsuleCastNonAlloc() : Scene 안의 모든 캡슐 콜라이더에 대한 Raycast를 통해 무엇과 충돌했는지 정보를 반환
        Physics.CapsuleCastNonAlloc(point1, point2, radius, velocity.normalized, _wallCastResults,
            radius * 0.04f, ~0, QueryTriggerInteraction.Ignore);

        var collides = _wallCastResults.Any(hit => hit.collider != null && hit.collider != _collider);

        if (!collides) return false;

        for (int i = 0; i < _wallCastResults.Length; i++)
            _wallCastResults[i] = new RaycastHit();

        return true;
    }

    // 점프
    private void Jump()
    {
        if (!_isGrounded || !input.Jump) return;

        _isGrounded = false;
        _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }


    // 발소리 재생
    private void PlayFootstepSounds()
    {
        // Vector3.sqrMagnitude : 벡터의 길이의 제곱값을 반환하여 움직이기 시작할 때 사운드 재생을 유도
        if (_isGrounded && _rigidbody.velocity.sqrMagnitude > 0.1f)
        {
            _audioSource.clip = input.Run ? runningSound : walkingSound;

            if (!_audioSource.isPlaying)
                _audioSource.Play();
        }
        else
        {
            if (_audioSource.isPlaying)
                _audioSource.Pause();
        }
    }

    // 부드러운 회전
    private class SmoothRotation
    {
        private float _current;
        private float _currentVelocity;

        public SmoothRotation(float startAngle)
        {
            _current = startAngle;
        }

        // 부드러운 회전 반환
        // SmoothDampAngle() : 시간이 지남에 따라 원하는 각도를 향해 점차적으로 각도를 변경
        public float Update(float target, float smoothTime)
        {
            return _current = Mathf.SmoothDampAngle(_current, target, ref _currentVelocity, smoothTime);
        }

        public float Current
        {
            set { _current = value; }
        }
    }

    // 부드러운 이동
    private class SmoothVelocity
    {
        private float _current;
        private float _currentVelocity;

        // 부드러운 이동속도 반환
        // SmoothDamp() : 시간이 지남에 따라 원하는 목표를 향해 점차적으로 벡터를 변환
        public float Update(float target, float smoothTime)
        {
            return _current = Mathf.SmoothDamp(_current, target, ref _currentVelocity, smoothTime);
        }

        public float Current
        {
            set { _current = value; }
        }
    }

    public void Resume()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            isESC = true;
            Cursor.lockState = CursorLockMode.None;
            ESCPanel.SetActive(true);
        }
        else
        {
            isESC = false;
            Cursor.lockState = CursorLockMode.Locked;
            ESCPanel.SetActive(false);
        }
    }

    public void Settings()
    {
        isSettings = true;
        ESCPanel.SetActive(false);
        SettingsPanel.SetActive(true);
    }

    public void Lobby()
    {
        NM.ShutdownRequest();
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void SensitivityValueChange()
    {
        mouseSensitivity = SensitivitySlider.value;
    }

    public void VolumeValueChange()
    {
        AudioListener.volume = VolumeSlider.value;
        Volume = VolumeSlider.value;
    }

    public void VolumeValueToggle()
    {
        AudioListener.volume = VolumeToggle.isOn ? Volume : 0;
        VolumeSlider.enabled = VolumeToggle.isOn;
    }

    public void Return()
    {
        isESC = false;
        isSettings = false;
        Cursor.lockState = CursorLockMode.Locked;
        SettingsPanel.SetActive(false);
    }

    // 조작 매핑
    [Serializable]
    private class FpsInput
    {
        [Tooltip("카메라를 y축을 중심으로 회전하도록 매핑된 가상 축의 이름"),
         SerializeField]
        private string rotateX = "Mouse X";

        [Tooltip("카메라를 x축을 중심으로 회전하도록 매핑된 가상 축의 이름"),
         SerializeField]
        private string rotateY = "Mouse Y";

        [Tooltip("캐릭터를 앞뒤로 이동하도록 매핑된 가상 축의 이름"),
         SerializeField]
        private string move = "Horizontal";

        [Tooltip("캐릭터를 좌우로 이동하도록 매핑된 가상 축의 이름"),
         SerializeField]
        private string leftRight = "Vertical";

        [Tooltip("달리기에 매핑된 가상 버튼 이름"),
         SerializeField]
        private string run = "Run";

        [Tooltip("점프에 매핑된 가상 버튼 이름"),
         SerializeField]
        private string jump = "Jump";

        // 카메라를 y축을 중심으로 회전하도록 매핑된 가상 축의 값을 반환
        public float RotateX
        {
            get { return Input.GetAxisRaw(rotateX); }
        }

        // 카메라를 x축을 중심으로 회전하도록 매핑된 가상 축의 값을 반환       
        public float RotateY
        {
            get { return Input.GetAxisRaw(rotateY); }
        }

        // 캐릭터를 앞뒤로 이동하도록 매핑된 가상 축의 값을 반환       
        public float Move
        {
            get { return Input.GetAxisRaw(move); }
        }

        // 캐릭터를 좌우로 이동하도록 매핑된 가상 축의 값을 반환        
        public float LeftRight
        {
            get { return Input.GetAxisRaw(leftRight); }
        }

        // Left Shift 버튼을 누르는 동안 매핑된 가상 버튼이 true를 반환         
        public bool Run
        {
            get { return Input.GetButton(run); }
        }

        /// Space bar를 누르면 매핑된 가상 버튼이 true를 반환
        public bool Jump
        {
            get { return Input.GetButtonDown(jump); }
        }
    }
}