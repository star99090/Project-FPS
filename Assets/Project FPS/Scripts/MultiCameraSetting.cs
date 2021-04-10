using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiCameraSetting : Bolt.EntityBehaviour<PlayerJoinedEvent>
{
    public GameObject[] HideObjects;
    public GameObject myNickName;

    public void SetHideObjects()
    {
        foreach(var go in HideObjects)
        {
            go.SetActive(false);
        }
    }

    public override void Attached()
    {
        var evnt = PlayerJoinedEvent.Create();
        evnt.Message = "PlayerJoin";
        evnt.Send();

        //if (entity.IsOwner)
          //  SetHideObjects();
    }

    private void Update()
    {
        if(entity.IsOwner && HideObjects[0].activeInHierarchy == false)
        {
            HideObjects[0].SetActive(true);
            HideObjects[1].SetActive(true);
        }
    }
}
