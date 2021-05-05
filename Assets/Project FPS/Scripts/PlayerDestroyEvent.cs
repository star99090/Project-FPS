using Bolt;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDestroyEvent : GlobalEventListener
{
    public override void OnEvent(DestroyRequestEvent evnt)
    {
        if (evnt.Entity.IsOwner)
            BoltNetwork.Destroy(evnt.Entity.gameObject);
    }
}
