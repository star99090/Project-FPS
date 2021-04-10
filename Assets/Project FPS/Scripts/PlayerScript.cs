using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static NetworkManager;

public class PlayerScript : Bolt.EntityBehaviour<IFPSPlayerState>
{
    public GameObject[] HideObjects;

    void Start() => NM.players.Add(entity);

    void OnDestroy() => NM.players.Remove(entity);

    public override void Attached()
    {
        CreateJoinedEvent();
        state.SetTransforms(state.playerTransform, transform);
    }

    void CreateJoinedEvent()
    {
        var evnt = JoinedEvent.Create();
        evnt.Send();
    }

    public void HideObject()
    {
        foreach(var go in HideObjects)
        {
            go.SetActive(false);
        }
    }
}
