using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bolt;
using static NetworkList;

public class NetworkCallbacks : GlobalEventListener
{
    [SerializeField] GameObject playerPrefab;

    [System.Obsolete]
    public override void SceneLoadLocalDone(string scene)
    {
        //titlePanel.SetActive(false);
        //lobbyCamera.SetActive(false);

        Vector3 spawnPos = new Vector3(Random.Range(-5f, 5f), 1.3f, 0f);
        BoltNetwork.Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        //if (myEntityPos != Vector3.zero) spawnPos = myEntityPos;

        //myEntity = BoltNetwork.Instantiate(playerPrefab, spawnPos, Quaternion.identity);//Quaternion.EulerAngles(myEntityRot));
        //myEntity.TakeControl();

        //if (BoltNetwork.IsServer)
        //  myEntity.GetComponent<FpsControllerLPFP>().SetIsServer(true);
    }

    public override void OnEvent(PlayerJoinedEvent evnt)
    {
        NL.RenewalPlayer();
        NL.MyCameraActive();
    }
}
