using UnityEngine;
using Bolt.Matchmaking;
using UnityEngine.UI;
using UdpKit;
#pragma warning disable CS0414

public class TitleLobbyManager : Bolt.GlobalEventListener
{
    public static TitleLobbyManager TLM { get; private set; }
    private void Awake() => TLM = this;

    public GameObject LogPanel;
    public Text LogText;
    public InputField SessionInput;
    public InputField NicknameInput;
    public string myNickname => NicknameInput.text;
    public string mySession => SessionInput.text;
    public GameObject cameraAudio;

    [Header("Sounds")]
    [Tooltip("로비 BGM"), SerializeField]
    private string bgm = "DayDreamSound - TTRM";

    [Tooltip("마우스 클릭 효과음"), SerializeField]
    AudioClip mouseClick;

    [Tooltip("마우스 오버 효과음"), SerializeField]
    AudioClip mouseOnOver;

    public void StartServer() => BoltLauncher.StartServer();
    public void StartClient() => BoltLauncher.StartClient();
    public void JoinSession() => BoltMatchmaking.JoinSession(SessionInput.text);

    public override void BoltStartDone()
    {
        if (BoltNetwork.IsServer)
        {
            PlayerPrefs.SetString("currentSession", SessionInput.text);
            BoltMatchmaking.CreateSession(sessionID: SessionInput.text, sceneToLoad: "FPSGame");
        }
    }

    public override void SessionListUpdated(Map<System.Guid, UdpSession> sessionList)
    {
        string log = "";
        foreach(var session in sessionList)
        {
            UdpSession photonSession = session.Value;
            if (photonSession.Source == UdpSessionSource.Photon)
                    log += $"{photonSession.HostName}\n";
        }
        LogText.text = log;
    }

    public void OnclickSearchingServer()
    {
        if (LogPanel.activeSelf)
        {
            LogPanel.SetActive(false);
        }
        else
            LogPanel.SetActive(true);
    }
    public void OnMouseOver() => cameraAudio.GetComponent<AudioSource>().PlayOneShot(mouseOnOver);
    public void OnMouseDown() => cameraAudio.GetComponent<AudioSource>().PlayOneShot(mouseClick);
}
