using System.Collections;
using UnityEngine;

public class CharacterAnimation : Bolt.EntityBehaviour<IFPSPlayerState>
{
	private Animator anim;

	[SerializeField] private BoltEntity myEntity;

	//private float lastFired;
	//public float fireRate;

	private void Awake()
	{
		anim = GetComponent<Animator>();
	}
	
    public override void Attached()
    {
		state.SetAnimator(anim);
		state.AddCallback("AnimPlay", AnimPlayCallback);
    }
	
	void AnimPlayCallback()
	{
		if (state.AnimPlay != "o")
		{
			state.Animator.Play(state.AnimPlay);
			Invoke("AnimPlayDelay", 0.03f);
		}
	}

	private void AnimPlayDelay() => state.AnimPlay = "o";

	private void Update()
	{
		if (myEntity.IsOwner)
		{
			float h = Input.GetAxis("Horizontal");
			float v = Input.GetAxis("Vertical");

			state.H = h;
			state.V = v;
		}

		anim.SetFloat("Horizontal", state.H);
		anim.SetFloat("Vertical", state.V);
		//if (!myEntity.IsOwner) return;


		/*
		//Run 45 up right ¢Ö
		if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.D))
		{
			preKey = true;
			state.UpRight = true;
			anim.SetBool("UpRight", state.UpRight);
		}
		else
		{
			preKey = false;
			state.UpRight = false;
			anim.SetBool("UpRight", state.UpRight);
		}

		//Run 45 back right ¢Ù
		if (Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.S))
		{
			preKey = true;
			state.BackRight = true;
			anim.SetBool("BackRight", state.BackRight);
		}
		else
		{
			preKey = false;
			state.BackRight = false;
			anim.SetBool("BackRight", state.BackRight);
		}

		//Run 45 back left ¢×
		if (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.A))
		{
			preKey = true;
			state.BackLeft = true;
			anim.SetBool("BackLeft", state.BackLeft);
		}
		else
		{
			preKey = false;
			state.BackLeft = false;
			anim.SetBool("BackLeft", state.BackLeft);
		}

		//Run 45 up left ¢Ø
		if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A))
		{
			preKey = true;
			state.UpLeft = true;
			anim.SetBool("UpLeft", state.UpLeft);
		}
		else
		{
			preKey = false;
			state.UpLeft = false;
			anim.SetBool("UpLeft", state.UpLeft);
		}

		//Run forward ¡è
		if (Input.GetKey(KeyCode.W) && preKey == false)
		{
			state.W = true;
			anim.SetBool("W", state.W);
		}
		else
		{
			state.W = false;
			anim.SetBool("W", state.W);
		}

		//Run strafe right ¡æ
		if (Input.GetKey(KeyCode.D) && preKey == false)
		{
			state.D = true;
			anim.SetBool("D", state.D);
		}
		else
		{
			state.D = false;
			anim.SetBool("D", state.D);
		}
		//Run backwards ¡é
		if (Input.GetKey(KeyCode.S) && preKey == false)
		{
			state.S = true;
			anim.SetBool("S", state.S);
		}
		else
		{
			state.S = false;
			anim.SetBool("S", state.S);
		}
		//Run strafe left ¡ç
		if (Input.GetKey(KeyCode.A) && preKey == false)
		{
			state.A = true;
			anim.SetBool("A", state.A);
		}
		else
		{
			state.A = false;
			anim.SetBool("A", state.A);
		}*/
	}

	public void FireAnim()
    {
		//anim.Play("Fire");
		state.AnimPlay = "Fire";
    }

	public void ReloadAnim()
    {
		//anim.Play("Reload");
		state.AnimPlay = "Reload";
    }
}