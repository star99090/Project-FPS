using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bolt;

public class NetworkManager : GlobalEventListener
{
    public static NetworkManager NM;
    private void Awake() => NM = this;

    public List<BoltEntity> players = new List<BoltEntity>();
    public BoltEntity myPlayer;

    public GameObject SpawnPrefab;

    public override void SceneLoadLocalDone(string scene, IProtocolToken token)
    {
        var spawnPos = new Vector3(Random.Range(-5, 5), 0, 0);
        BoltNetwork.Instantiate(SpawnPrefab, spawnPos, Quaternion.identity);
    }

    public override void OnEvent(JoinedEvent evnt)
    {
        Invoke("JoinedEventDelay", 0.5f);
    }

    void JoinedEventDelay()
    {
        foreach (var player in players)
        {
            if (player != myPlayer)
                player.GetComponent<PlayerScript>().HideObject();
        }
    }
}
    /*
    public static NetworkManager Instance { get; private set; }
    void Awake() => Instance = this;

    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject titlePanel;
    [SerializeField] GameObject lobbyCamera;

    [SerializeField] List<BoltEntity> entities;
    [SerializeField] BoltEntity myEntity;
    [SerializeField] Vector3 myEntityPos;
    [SerializeField] Vector3 myEntityRot;
    [SerializeField] InputField nickInput;
    public string myNickName => nickInput.text;
    [SerializeField] List<GameObject> players;
    [SerializeField] GameObject myPlayer;

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

    void BoltShutdownCallback()
    {
        if (isMyHost)
            BoltLauncher.StartServer();
        else
            BoltLauncher.StartClient();
    }

    public override void SceneLoadLocalDone(string scene, IProtocolToken token)
    {
        titlePanel.SetActive(false);
        lobbyCamera.SetActive(false);

        Vector3 spawnPos = new Vector3(Random.Range(-5f, 5f), 1.3f, Random.Range(-5f, 5f));

        if (myEntityPos != Vector3.zero) spawnPos = myEntityPos;

        myEntity = BoltNetwork.Instantiate(playerPrefab, spawnPos, Quaternion.identity);//Quaternion.EulerAngles(myEntityRot));
        myEntity.TakeControl();

        //if (BoltNetwork.IsServer)
          //  myEntity.GetComponent<FpsControllerLPFP>().SetIsServer(true);
    }


    void FixedUpdate()
    {
        if (myEntity != null)
        {
            myEntityPos = myEntity.transform.position;
            myEntityRot = new Vector3(myEntity.transform.rotation.x, myEntity.transform.rotation.y, myEntity.transform.rotation.z);
        }
    }
    
    public override void Connected(BoltConnection connection)
    {
        var evnt = HostMigrationEntityChangeEvent.Create();
        evnt.isServer = false;
        evnt.connectionId = (int)connection.ConnectionId;
        evnt.Send();
    }

    public override void BoltShutdownBegin(AddCallback registerDoneCallback, UdpConnectionDisconnectReason disconnectReason) => registerDoneCallback(BoltShutdownCallback);

    public override void Disconnected(BoltConnection connection) => StartCoroutine(UpdateEntity());

    IEnumerator UpdateEntity()
    {
        yield return new WaitForSeconds(0.1f);
        var myEntityUpdate = HostMigrationEntityChangeEvent.Create();
        myEntityUpdate.position = transform.position;
        myEntityUpdate.rotation = transform.eulerAngles;
        myEntityUpdate.Send();
    }

    void RenewalPlayers()
    {
        players = new List<GameObject>();
        foreach(var player in GameObject.FindGameObjectsWithTag("FPSPlayer"))
        {
            players.Add(player);
            if (player.GetComponent<BoltEntity>().IsOwner)
                myPlayer = player;
        }
    }

    void MyCameraActive()
    {
        foreach (var player in players)
        {
            bool owner = player.GetComponent<BoltEntity>().IsOwner;
            if (!owner)
            {
                player.GetComponent<MultiCameraSetting>().SetHideObjects();
                player.GetComponent<MultiCameraSetting>().myNickName.SetActive(true);
            }
            else
                player.GetComponent<MultiCameraSetting>().myNickName.SetActive(false);
        }
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
    /*
    public override void OnEvent(PlayerJoinedEvent evnt)
    {
        RenewalPlayers();
        MyCameraActive();
    }*/
