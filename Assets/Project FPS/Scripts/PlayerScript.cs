using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static NetworkManager;

public class PlayerScript : Bolt.EntityBehaviour<IFPSPlayerState>
{
    public GameObject[] HideObjects;
    public GameObject NicknameCanvas;

    void Start() => NM.players.Add(entity);

    void OnDestroy() => NM.players.Remove(entity);

    public override void Attached()
    {
        var evnt = JoinedEvent.Create();
        evnt.Send();
        state.SetTransforms(state.playerTransform, transform);
    }

    public void HideObject()
    {
        foreach(var go in HideObjects)
            go.SetActive(false);
    }

    public void NicknameSet(bool a) => NicknameCanvas.SetActive(a);
}
