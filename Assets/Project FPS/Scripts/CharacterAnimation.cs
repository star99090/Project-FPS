using System.Collections;
using UnityEngine;

public class CharacterAnimation : Bolt.EntityBehaviour<IFPSPlayerState>
{
	private Animator anim;

	[SerializeField] private BoltEntity myEntity;

	private void Awake() => anim = GetComponent<Animator>();
	public void FireAnim() => state.AnimPlay = "Fire";
	public void ReloadAnim() => state.AnimPlay = "Reload";
	private void AnimPlayDelay() => state.AnimPlay = "o";
	
	void AnimPlayCallback()
	{
		if (state.AnimPlay != "o")
		{
			state.Animator.Play(state.AnimPlay);
			Invoke("AnimPlayDelay", 0.03f);
		}
	}

	public override void Attached()
    {
		state.SetAnimator(anim);
		state.AddCallback("AnimPlay", AnimPlayCallback);
    }
	
	private void Update()
	{
		if (myEntity.IsOwner)
		{
			state.H = Input.GetAxis("Horizontal");
			state.V = Input.GetAxis("Vertical");
		}

		anim.SetFloat("Horizontal", state.H);
		anim.SetFloat("Vertical", state.V);
	}
}