using UnityEngine;
using System.Collections;

public class CasingScriptBefore : MonoBehaviour {

	[Header("Force X")]
	[Tooltip("X축에 가할 최소 힘")]
	public float minimumXForce;		
	[Tooltip("X축에 가할 최대 힘")]
	public float maximumXForce;

	[Header("Force Y")]
	[Tooltip("Y축에 가할 최소 힘")]
	public float minimumYForce;
	[Tooltip("Y축에 가할 최대 힘")]
	public float maximumYForce;

	[Header("Force Z")]
	[Tooltip("Z축에 가할 최소 힘")]
	public float minimumZForce;
	[Tooltip("Z축에 가할 최대 힘")]
	public float maximumZForce;

	[Header("Rotation Force")]
	[Tooltip("최소 회전 값")]
	public float minimumRotation;
	[Tooltip("최대 회전 값")]
	public float maximumRotation;

	[Header("Despawn Time")]
	[Tooltip("탄피 파괴까지 걸리는 시간")]
	public float despawnTime;

	[Header("Audio")]
	public AudioClip[] casingSounds;
	public AudioSource audioSource;

	[Header("Spin Settings")]
	[Tooltip("탄피 회전 속도")]
	public float speed = 2500.0f;

	private void Awake () 
	{
		// 탄피의 랜덤 회전(토크) 부여
		GetComponent<Rigidbody>().AddRelativeTorque (
			Random.Range(minimumRotation, maximumRotation), //X Axis
			Random.Range(minimumRotation, maximumRotation), //Y Axis
			Random.Range(minimumRotation, maximumRotation)  //Z Axis
			* Time.deltaTime);

		// 탄피가 튕겨져 나가도록 힘 부여
		GetComponent<Rigidbody>().AddRelativeForce (
			Random.Range (minimumXForce, maximumXForce),  //X Axis
			Random.Range (minimumYForce, maximumYForce),  //Y Axis
			Random.Range (minimumZForce, maximumZForce)); //Z Axis		     
	}

	private void Start () 
	{
		StartCoroutine(RemoveCasing());
		
		// 탄피의 회전각 랜덤 설정
		transform.rotation = Random.rotation;
		
		StartCoroutine(PlaySound());
	}

	private void FixedUpdate () 
	{
		transform.Rotate (Vector3.right, speed * Time.deltaTime);
		transform.Rotate (Vector3.down, speed * Time.deltaTime);
	}

	private IEnumerator PlaySound () 
	{
		yield return new WaitForSeconds (Random.Range(0.25f, 0.85f));

		audioSource.clip = casingSounds
			[Random.Range(0, casingSounds.Length)];

		audioSource.Play();
	}

	private IEnumerator RemoveCasing () 
	{
		yield return new WaitForSeconds (despawnTime);

		Destroy(gameObject);
	}
}