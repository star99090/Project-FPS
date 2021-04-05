using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Bolt;
using Bolt.Matchmaking;
using System.Linq;
using UdpKit;

public class NetworkManager : GlobalEventListener
{
    public static NetworkManager Instance { get; private set; }
    void Awake() => Instance = this;

    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject titlePanel;

    [SerializeField] List<BoltEntity> entities;
    [SerializeField] BoltEntity myEntity;
    [SerializeField] InputField nickInput;
    public string myNickName => nickInput.text;

    bool isMyHost;

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

        Vector3 spawnPos = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));

        myEntity = BoltNetwork.Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        myEntity.TakeControl();

        if (BoltNetwork.IsServer)
            myEntity.GetComponent<Player>().SetIsServer(true);
    }

    void UpdateEntity()
    {
        HostMigrationEntityChangeEvent.Create().Send();
    }

    public override void Connected(BoltConnection connection)
    {
        var evnt = HostMigrationEntityChangeEvent.Create();
        evnt.isServer = false;
        evnt.connectionId = (int)connection.ConnectionId;
        if (myEntity != null)
        {
            evnt.position = myEntity.GetComponent<Player>().state.transform.Position;
            evnt.rotation = new Vector3(myEntity.GetComponent<Player>().state.transform.Rotation.x,
                                        myEntity.GetComponent<Player>().state.transform.Rotation.y,
                                        myEntity.GetComponent<Player>().state.transform.Rotation.z);
        }
        evnt.Send();
    }
    public override void Disconnected(BoltConnection connection)
    { 
        Invoke("UpdateEntity", 0.1f);
    }

    public override void OnEvent(HostMigrationEntityChangeEvent evnt)
    {
        entities = BoltNetwork.Entities.ToList();
        for(int i=0;i<entities.Count;i++)
        {
            if (!entities[i].GetComponent<Player>().state.isServer)
            {
                isMyHost = entities[i].IsOwner;
                return;
            }
        }
    }

    public override void BoltShutdownBegin(AddCallback registerDoneCallback, UdpConnectionDisconnectReason disconnectReason)
    {
        BoltLog.Info("BoltShutdownBegin");
        registerDoneCallback(BoltShutdownCallback);
    }

    void BoltShutdownCallback()
    {
        if (isMyHost)
            BoltLauncher.StartServer();
        else
            BoltLauncher.StartClient();
    }
}
