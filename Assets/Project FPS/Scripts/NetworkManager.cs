using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bolt;
using Bolt.Matchmaking;
using UdpKit;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#pragma warning disable CS0618

public class NetworkManager : GlobalEventListener
{
    public static NetworkManager NM { get; set; }
    private void Awake() => NM = this;

    [Header("About Player")]
    public List<BoltEntity> players = new List<BoltEntity>();
    public BoltEntity myPlayer;
    [Space(10)]
    public GameObject playerPrefab;
    [Space(10)]
    [SerializeField] BoltEntity entity;
    [SerializeField] Vector3 entityPos;
    [SerializeField] Vector3 entityRot;

    [Header("About Kill")]
    [SerializeField] Text killLogText;
    public int killLogCount = 0;
    private float killLogTimer;
    private string preKiller = "";
    private string preKiller2 = "";
    private string preKiller3 = "";
    private string preVictim = "";
    private string preVictim2 = "";
    private string preVictim3 = "";
    private string firstPlayer = "Nobody";
    private int firstPlayerScore = 0;

    [Header("Respawn Points")]
    public Transform[] respawnPoint;

    [Header("About Server Canvas")]
    public GameObject resultPanel;
    public GameObject tabPanel;
    public Text winnerNickname;
    public Text playersScore;
    public bool isResult = false;

    private string currentSession;
    private bool isMyHost;

    public override void SceneLoadLocalDone(string scene, IProtocolToken token)
    {
        int randomPos = Random.Range(0, respawnPoint.Length);

        Vector3 spawnPos = respawnPoint[randomPos].position;

        if (entityPos != Vector3.zero)
            spawnPos = entityPos;

        entity = BoltNetwork.Instantiate(playerPrefab, spawnPos,
            Quaternion.EulerAngles(entityRot));
        entity.TakeControl();

        if (BoltNetwork.IsServer)
            entity.GetState<IFPSPlayerState>().isServer = true;
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
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != myPlayer)
            {
                players[i].GetComponent<PlayerSubScript>().HideObject();
                players[i].GetComponent<PlayerSubScript>().MyBodySet();
            }
            else
                players[i].GetComponent<PlayerSubScript>().MySet();
        }
    }

    public override void OnEvent(HostMigrationEvent evnt)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].GetComponent<PlayerSubScript>().state.isServer)
            {
                isMyHost = players[i].IsOwner;
                return;
            }
        }
        entityPos = evnt.position;
        entityRot = evnt.rotation;
    }

    public override void OnEvent(PlayerHitEvent evnt)
    {
        if (myPlayer == evnt.targetEntity)
        {
            myPlayer.GetComponent<PlayerSubScript>().HealthChange(evnt.damage, evnt.attacker, evnt.attackerEntity);
        }
    }

    public override void OnEvent(KillEvent evnt)
    {
        if (!evnt.isSuicide)
        {
            evnt.attackerEntity.GetComponent<PlayerSubScript>().myKillScore += 1;
            evnt.attackerEntity.GetComponent<Player>().isWeaponChange = true;
            evnt.attackerEntity.GetComponent<PlayerSubScript>().UpdateMyScore();

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].GetComponent<PlayerSubScript>().myKillScore > firstPlayerScore)
                {
                    firstPlayerScore = players[i].GetComponent<PlayerSubScript>().myKillScore;
                    firstPlayer = players[i].GetComponent<PlayerSubScript>().nickname.text;
                }
            }

            for (int i = 0; i < players.Count; i++)
            {
                players[i].GetComponent<PlayerSubScript>().firstPlayerText.text = firstPlayer;
                players[i].GetComponent<PlayerSubScript>().firstScoreText.text = firstPlayerScore.ToString();
            }

            if (evnt.attackerEntity.GetComponent<PlayerSubScript>().myKillScore == 1)
            {
                isResult = true;

                winnerNickname.text = evnt.killer;
                myPlayer.GetComponent<PlayerSubScript>().ProgressSub(false);

                ProgressScore();

                resultPanel.SetActive(true);

                if (BoltNetwork.IsServer)
                    StartCoroutine(Shutdown(5.1f));
                else
                    StartCoroutine(Shutdown(5.0f));
            }
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

    private void ProgressScore()
    {
        playersScore.text = "";

        List<string> nickList = new List<string>();
        List<int> scoreList = new List<int>();

        int scoreTemp = 0;
        string nickTemp = "";

        for (int i = 0; i < players.Count; i++)
        {
            nickList.Add(players[i].GetComponent<Player>().nicknameText.text);
            scoreList.Add(players[i].GetComponent<PlayerSubScript>().myKillScore);
        }

        // 닉네임과 점수 정렬
        for (int i = 0; i < scoreList.Count - 1; i++)
        {
            for (int j = i + 1; j < scoreList.Count; j++)
            {
                if (scoreList[i] < scoreList[j])
                {
                    scoreTemp = scoreList[i];
                    scoreList[i] = scoreList[j];
                    scoreList[j] = scoreTemp;

                    nickTemp = nickList[i];
                    nickList[i] = nickList[j];
                    nickList[j] = nickTemp;
                }
            }
        }

        for (int i = 0; i < nickList.Count; i++)
            playersScore.text += nickList[i] + " : " + scoreList[i] + "Kill\n";
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
        if (myPlayer != null)
        {
            entityPos = myPlayer.transform.position;
            entityRot = myPlayer.transform.rotation.eulerAngles;
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

    public void ShutdownRequest()
    {
        StartCoroutine(Shutdown());
    }

    IEnumerator UpdateEntityAndSessionName()
    {
        yield return null;
        var myUpdate = HostMigrationEvent.Create();
        myUpdate.position = myPlayer.transform.position;
        myUpdate.rotation = myPlayer.transform.rotation.eulerAngles;
        myUpdate.sessionName = currentSession;
        myUpdate.Send();
    }

    IEnumerator Shutdown(float delay = 0f)
    {
        yield return new WaitForSeconds(delay);
        BoltNetwork.Shutdown();
        yield return new WaitForEndOfFrame();
        SceneManager.LoadScene("Title&Lobby");
    }

    public override void Disconnected(BoltConnection connection) => StartCoroutine(UpdateEntityAndSessionName());
}