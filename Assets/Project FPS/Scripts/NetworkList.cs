using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkList : Bolt.GlobalEventListener
{
    public static NetworkList NL;
    private void Awake() => NL = this;

    public List<GameObject> players;
    public GameObject myPlayer;

    public void RenewalPlayer()
    {
        players = new List<GameObject>();
        foreach(var player in GameObject.FindGameObjectsWithTag("FPSPlayer"))
        {
            players.Add(player);
            if (player.GetComponent<BoltEntity>().IsOwner)
                myPlayer = player;
        }
    }

    public void MyCameraActive()
    {
        foreach (var player in players)
        {
            bool owner = player.GetComponent<BoltEntity>().IsOwner;
            if (owner)
                player.GetComponent<PlayerEvent>().SetHideObjects();
        }
    }
}
