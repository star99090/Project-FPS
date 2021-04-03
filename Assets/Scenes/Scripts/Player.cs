using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Bolt;

public class Player : EntityBehaviour<IPlayerState>
{
    
    [SerializeField] float speed; //6
    [Range(0f,1f)]
    [SerializeField] float rotationLerp; //0.08
    
    [SerializeField] float jumpPower; //12
    [SerializeField] float jumpCoolTime; //0.8

    [SerializeField] Text nameText;
    [SerializeField] Transform playerCanvas;

    Rigidbody rigid;

    bool isGround;
    bool jumpable = true;
    public Vector3 tempPosition => state.transform.Position;

    public void SetIsServer(bool isServer) => state.isServer = isServer;
    public override void Attached()
    {
        state.SetTransforms(state.transform, transform);
        rigid = GetComponent<Rigidbody>();
    }

    public override void SimulateController()
    {
        IPlayerCommandInput input = PlayerCommand.Create();
        input.up = Input.GetKey(KeyCode.W);
        input.left = Input.GetKey(KeyCode.A);
        input.down = Input.GetKey(KeyCode.S);
        input.right = Input.GetKey(KeyCode.D);
        input.jump = Input.GetKey(KeyCode.Space);
        entity.QueueInput(input);
    }

    //입력받으면 부르는 콜백
    public override void ExecuteCommand(Command command, bool resetState)
    {
        PlayerCommand cmd = (PlayerCommand)command;

        if (resetState)
        {
            cmd.Result.velocity = rigid.velocity;
            cmd.Result.angularVelocity = rigid.angularVelocity;
        }
        else
        {
            Vector3 dir = Vector3.zero;

            if (cmd.Input.up)
            {
                dir += transform.forward;
                //회전 로직 필요
                transform.rotation = Quaternion.Lerp(transform.rotation,
                    Quaternion.LookRotation(transform.forward), rotationLerp);
            }
            else if (cmd.Input.down)
            {
                dir += -transform.forward;
                //transform.rotation = Quaternion.Lerp(transform.rotation,
                //    Quaternion.LookRotation(-transform.forward), rotationLerp);
            }

            if (cmd.Input.right)
            {
                dir += transform.right;
                /*
                Vector3 right = Quaternion.Euler(0, 90, 0) * transform.forward;
                dir += right;
                transform.rotation = Quaternion.Lerp(transform.rotation,
                    Quaternion.LookRotation(right), rotationLerp);
                */
            }
            else if (cmd.Input.left)
            {
                dir += -transform.right;
                /*
                Vector3 left = Quaternion.Euler(0, -90, 0) * transform.forward;
                dir += left;
                transform.rotation = Quaternion.Lerp(transform.rotation,
                    Quaternion.LookRotation(left), rotationLerp);
                */
            }


            Vector3 tempVelocity = dir.normalized * speed + Vector3.up * rigid.velocity.y;

            if (cmd.Input.jump && jumpable && isGround)
            {
                tempVelocity += Vector3.up * jumpPower;
                Invoke(nameof(JumpCoolTimeDelay), jumpCoolTime);
                jumpable = false;
            }

            rigid.velocity = tempVelocity;

            cmd.Result.velocity = rigid.velocity;
            cmd.Result.angularVelocity = rigid.angularVelocity;
            //cmd.Result.po 
        }
    }

    void JumpCoolTimeDelay() => jumpable = true;
    void Update()
    {
        if (!entity.IsOwner) return;

        isGround = Physics.Raycast(transform.position + Vector3.up, Vector3.down, 1.05f);
    }
    void LateUpdate() => playerCanvas.rotation = transform.rotation;
}
