using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bolt;
using Bolt.Matchmaking;
using UdpKit;
using System.Linq;
using UnityEngine.UI;
#pragma warning disable CS0618

public class NetworkManager : GlobalEventListener
{
    public static NetworkManager NM { get; set; }
    private void Awake() {
        NM = this;
    }

    public List<BoltEntity> players = new List<BoltEntity>();
    public BoltEntity myPlayer;

    public GameObject SpawnPrefab;
    private string currentSession;
    bool isMyHost;

    [SerializeField] Text killLogText;
    [SerializeField] List<BoltEntity> entities = new List<BoltEntity>();
    [SerializeField] BoltEntity myEntity;
    [SerializeField] Vector3 myEntityPos;
    [SerializeField] Vector3 myEntityRot;

    public override void SceneLoadLocalDone(string scene, IProtocolToken token)
    {
        var spawnPos = new Vector3(Random.Range(-5, 5), 0, 0);
        
        if (myEntityPos != Vector3.zero)
            spawnPos = myEntityPos;

        myEntity = BoltNetwork.Instantiate(SpawnPrefab, spawnPos,
            Quaternion.EulerAngles(myEntityRot));
        myEntity.TakeControl();

        if (BoltNetwork.IsServer)
            myEntity.GetState<IFPSPlayerState>().isServer = true;
    }

    public override void BoltStartDone()
    {
        if (BoltNetwork.IsServer)
        {
            currentSession = PlayerPrefs.GetString("currentSession");
            BoltMatchmaking.CreateSession(sessionID: currentSession, sceneToLoad: "FPSGame");
        }
        else
            BoltMatchmaking.JoinSession(currentSession);
    }

    void BoltShutdownCallback()
    {
        if (isMyHost)
            BoltLauncher.StartServer();
        else
            BoltLauncher.StartClient();
    }

    public override void BoltShutdownBegin(AddCallback registerDoneCallback,
        UdpConnectionDisconnectReason disconnectReason)
    {
        registerDoneCallback(BoltShutdownCallback);
    }

    public override void OnEvent(JoinedEvent evnt)
    {
        Invoke("JoinedEventDelay", 0.25f);
    }

    public override void OnEvent(HostMigrationEvent evnt)
    {
        entities = BoltNetwork.Entities.ToList();
        for (int i = 0; i < entities.Count; i++)
        {
            if (!entities[i].GetComponent<PlayerSubScript>().state.isServer)
            {
                isMyHost = entities[i].IsOwner;
                return;
            }
        }
        myEntityPos = evnt.position;
        myEntityRot = evnt.rotation;
    }

    public override void OnEvent(PlayerHitEvent evnt)
    {
        if (myPlayer = evnt.targetEntity)
        {
            myPlayer.GetComponent<PlayerSubScript>().HealthChange(evnt.damage, evnt.attacker);
        }
    }

    void JoinedEventDelay()
    {
        foreach (var player in players)
        {
            if (player != myPlayer)
                player.GetComponent<PlayerSubScript>().HideObject();
            else
                player.GetComponent<PlayerSubScript>().NicknameSet(false);
        }
    }

    private void FixedUpdate()
    {
        if (myEntity != null)
        {
            myEntityPos = myEntity.transform.position;
            myEntityRot = myEntity.transform.rotation.eulerAngles;
        }        
    }

    public override void Connected(BoltConnection connection)
    {
        var evnt = HostMigrationEvent.Create();
        evnt.isServer = false;
        evnt.connectionId = (int)connection.ConnectionId;
        currentSession = evnt.sessionName;
        evnt.Send();
    }

    IEnumerator UpdateEntityAndSessionName()
    {
        yield return new WaitForSeconds(0.1f);
        var myUpdate = HostMigrationEvent.Create();
        myUpdate.position = myEntity.transform.position;
        myUpdate.rotation = myEntity.transform.rotation.eulerAngles;
        myUpdate.sessionName = currentSession;
        myUpdate.Send();
    }
    
    public override void Disconnected(BoltConnection connection) => StartCoroutine(UpdateEntityAndSessionName());
}