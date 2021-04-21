using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static NetworkManager;

public class PlayerSubScript : Bolt.EntityBehaviour<IFPSPlayerState>
{
    public GameObject[] HideObjects;
    public GameObject NicknameCanvas;
    public Transform arm;
    public Text HealthText;

    public int hp = 100;

    void Start() => NM.players.Add(entity);
    void OnDestroy() => NM.players.Remove(entity);

    public override void Attached()
    {
        var evnt = JoinedEvent.Create();
        evnt.Send();
        state.health = 100;
        state.SetTransforms(state.armTransform, arm);
        state.SetTransforms(state.playerTransform, transform);
        state.AddCallback("health", HealthCallback);
    }

    public void HideObject()
    {
        foreach (var go in HideObjects)
            go.SetActive(false);
    }

    public void NicknameSet(bool a) => NicknameCanvas.SetActive(a);

    public void HealthChange(int damage) => state.health -= damage;

    void HealthCallback()
    {
        hp = state.health;
        HealthText.text = hp.ToString();
    }
}
