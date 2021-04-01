using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Bolt;
using Bolt.Matchmaking;

public class NetworkManager : GlobalEventListener
{
    public static NetworkManager Instance { get; private set; }
    void Awake() => Instance = this;

    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject titlePanel;
    public InputField nameInput;

    void Start() => titlePanel.SetActive(true);

    public void StartServer() => BoltLauncher.StartServer();
    public void StartClient() => BoltLauncher.StartClient();

    public override void BoltStartDone()
    {
        if (BoltNetwork.IsServer)
            BoltMatchmaking.CreateSession("room");
        else
            BoltMatchmaking.JoinSession("room");
    }

    public override void SceneLoadLocalDone(string scene, IProtocolToken token)
    {
        titlePanel.SetActive(false);
        Debug.Log("Àü");
        Vector3 spawnPos = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
        BoltEntity entity = BoltNetwork.Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        Debug.Log("ÈÄ");
        entity.TakeControl();
    }
}
