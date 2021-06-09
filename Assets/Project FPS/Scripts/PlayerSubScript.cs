using UnityEngine;
using UnityEngine.UI;
using static NetworkManager;
#pragma warning disable CS0618

public class PlayerSubScript : Bolt.EntityBehaviour<IFPSPlayerState>
{
    public GameObject[] HideObjects;
    public GameObject[] MyBody;

    public GameObject NicknameCanvas;
    public GameObject GunCamera;
    public GameObject MinimapCamera;
    public GameObject Minimap;
    public GameObject GoalText;
    public Text nickname;
    public Text firstPlayerText;
    public Text firstScoreText;
    public Text myScoreText;
    public GameObject KillScorePanel;

    public Transform armsParent;
    public Text HealthText;
    public int myKillScore = 0;
    public int death = 0;

    string attacker;
    BoltEntity attackerEntity;

    private void OnDestroy() => NM.players.Remove(entity);
    public void UpdateMyScore() => myScoreText.text = myKillScore.ToString();
    public void UpdateMyDeath() => state.death = death;

    public override void Attached()
    {
        NM.players.Add(entity);

        var evnt = JoinedEvent.Create();
        evnt.Send();

        state.health = 100;
        state.death = 0;
        state.SetTransforms(state.armTransform, armsParent);
        state.SetTransforms(state.playerTransform, transform);
        state.AddCallback("health", HealthCallback);
    }

    public void HideObject()
    {
        for (int i = 0; i < HideObjects.Length; i++)
            HideObjects[i].SetActive(false);
    }

    public void MySet()
    {
        NicknameCanvas.SetActive(false);
        GunCamera.SetActive(true);
        MinimapCamera.SetActive(true);
    }

    public void BodyLayerChange()
    {
        for (int i = 0; i < MyBody.Length; i++)
            MyBody[i].layer = 0;
    }

    public void HealthChange(int damage, string attackers, BoltEntity aEntity)
    {
        state.health -= damage;
        attacker = attackers;
        attackerEntity = aEntity;
    }

    void HealthCallback()
    {
        HealthText.text = state.health.ToString();

        if (state.health <= 0)
            Respawn();
    }

    private void Respawn()
    {
        var evnt = KillEvent.Create();
        evnt.killer = attacker;
        evnt.victim = nickname.text;
        evnt.attackerEntity = attackerEntity;
        evnt.vitimEntity = entity;

        if (attacker == nickname.text)
            evnt.isSuicide = true;
        else
            evnt.isSuicide = false;

        if (evnt.isSuicide)
            state.health = 1;
        else
            state.health = 100;

        int RP = Random.Range(0, NM.respawnPoint.Length);
        transform.position = NM.respawnPoint[RP].position;
        transform.rotation = NM.respawnPoint[RP].rotation;

        evnt.Send();
    }

    public void ProgressSub(bool onOff)
    {
        if (NM.isResult)
        {
            GoalText.SetActive(onOff);
            Minimap.SetActive(onOff);
        }
        KillScorePanel.SetActive(onOff);
    }
}