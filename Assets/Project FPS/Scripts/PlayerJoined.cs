using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerJoined : Bolt.EntityBehaviour<IPlayerState>
{/*
    public GameObject[] HideObjects;
    //public GameObject myNickName;

    public void SetHideObjects()
    {
        foreach (var go in HideObjects)
        {
            go.SetActive(false);
        }
    }
    */
    public override void Attached()
    {
        var evnt = PlayerJoinedEvent.Create();
        evnt.Message = "PlayerJoin";
        evnt.Send();
    }
}
