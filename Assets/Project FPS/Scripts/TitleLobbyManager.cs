using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bolt.Matchmaking;
using UnityEngine.UI;
using UdpKit;
using Bolt;

public class TitleLobbyManager : Bolt.GlobalEventListener
{
    public Text LogText;
    public InputField SessionInput;
    //public InputField NicknameInput;


    public void StartServer() => BoltLauncher.StartServer();
    public void StartClient() => BoltLauncher.StartClient();
    public void JoinSession() => BoltMatchmaking.JoinSession(SessionInput.text);
    public override void BoltStartDone() => BoltMatchmaking.CreateSession(sessionID: SessionInput.text, sceneToLoad: "FPSGame");

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
}
