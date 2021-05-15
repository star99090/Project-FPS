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
    private Coroutine coroutine;
    public static NetworkManager NM { get; set; }
    private void Awake() => NM = this;

    public List<BoltEntity> players = new List<BoltEntity>();
    public BoltEntity myPlayer;

    public GameObject SpawnPrefab;
    private string currentSession;
    public int killLogCount = 0;
    float killLogTimer;
    bool isMyHost;
    bool isReset = false;
    private string preKiller = "";
    private string preKiller2 = "";
    private string preKiller3 = "";
    private string preVictim = "";
    private string preVictim2 = "";
    private string preVictim3 = "";
    private string firstPlayer = "Nobody";
    private int firstPlayerScore = 0;

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

    void JoinedEventDelay()
    {
        foreach (var player in players)
        {
            if (player != myPlayer)
            {
                player.GetComponent<PlayerSubScript>().HideObject();
                player.GetComponent<PlayerSubScript>().MyBodySet();
            }
            else
                player.GetComponent<PlayerSubScript>().NicknameSet();
        }
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
            myPlayer.GetComponent<PlayerSubScript>().HealthChange(evnt.damage, evnt.attacker, evnt.attackerEntity);
        }
    }

    public override void OnEvent(KillEvent evnt)
    {
        evnt.attackerEntity.GetComponent<PlayerSubScript>().myKillScore += 1;
        evnt.attackerEntity.GetComponent<Player>().killScore++;
        evnt.attackerEntity.GetComponent<Player>().isWeaponChange = true;
        evnt.attackerEntity.GetComponent<PlayerSubScript>().UpdateMyScore();

        foreach (var player in players)
        {
            if (player.GetComponent<PlayerSubScript>().myKillScore > firstPlayerScore)
            {
                firstPlayerScore = player.GetComponent<PlayerSubScript>().myKillScore;
                firstPlayer = player.GetComponent<PlayerSubScript>().nickname.text;
            }
        }

        foreach(var player in players)
        {
            player.GetComponent<PlayerSubScript>().firstPlayerText.text = firstPlayer;
            player.GetComponent<PlayerSubScript>().firstScoreText.text = firstPlayerScore.ToString();
        }

        killLogTimer = 0f;
        switch (killLogCount)
        {
            case 0:
                killLogCount++;
                SaveKillLogState(evnt.killer, evnt.victim);
                killLogText.text = preKiller + " Kills " + preVictim;
                break;
            case 1:
                killLogCount++;
                SaveKillLogState(evnt.killer, evnt.victim);
                killLogText.text = preKiller2 + " Kills " + preVictim2 + "\n"
                    + preKiller + " Kills " + preVictim;
                break;
            case 2:
                killLogCount++;
                SaveKillLogState(evnt.killer, evnt.victim);
                killLogText.text = preKiller3 + " Kills " + preVictim3 + "\n"
                    + preKiller2 + " Kills " + preVictim2 + "\n"
                    + preKiller + " Kills " + preVictim;
                break;
            case 3:
                SaveKillLogState(evnt.killer, evnt.victim);
                killLogText.text = preKiller3 + " Kills " + preVictim3 + "\n"
                    + preKiller2 + " Kills " + preVictim2 + "\n"
                    + preKiller + " Kills " + preVictim;
                break;
        }
    }

    private void SaveKillLogState(string killer, string victim)
    {
        preKiller3 = preKiller2;
        preVictim3 = preVictim2;
        preKiller2 = preKiller;
        preVictim2 = preVictim;
        preKiller = killer;
        preVictim = victim;
    }

    private void Update()
    {
        if (killLogCount > 0)
        {
            killLogTimer += Time.deltaTime;
            if (killLogTimer >= 3.0f)
            {
                killLogTimer = 0;
                killLogCount--;

                switch (killLogCount)
                {
                    case 0:
                        killLogText.text = "";
                        break;
                    case 1:
                        killLogText.text = preKiller + " Kills " + preVictim;
                        break;
                    case 2:
                        killLogText.text = preKiller2 + " Kills " + preVictim2 + "\n"
                            + preKiller + " Kills " + preVictim;
                        break;
                }
            }
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
        yield return null;
        var myUpdate = HostMigrationEvent.Create();
        myUpdate.position = myEntity.transform.position;
        myUpdate.rotation = myEntity.transform.rotation.eulerAngles;
        myUpdate.sessionName = currentSession;
        myUpdate.Send();
    }

    public override void Disconnected(BoltConnection connection) => StartCoroutine(UpdateEntityAndSessionName());
}