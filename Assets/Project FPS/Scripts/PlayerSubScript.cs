using UnityEngine;
using UnityEngine.UI;
using static NetworkManager;
#pragma warning disable CS0618

public class PlayerSubScript : Bolt.EntityBehaviour<IFPSPlayerState>
{
    public GameObject[] HideObjects;
    public GameObject[] MyBody;
    public GameObject[] MyCharacterModelWeapon;

    public GameObject NicknameCanvas;
    public GameObject GunCamera;
    public GameObject Minimap;
    public Text nickname;
    public Text firstPlayerText;
    public Text firstScoreText;
    public Text myScoreText;

    public Transform armsParent;
    public Text HealthText;
    public int myKillScore = 0;

    string attacker;
    BoltEntity attackerEntity;

    private void OnDestroy() => NM.players.Remove(entity);
    public void UpdateMyScore() => myScoreText.text = myKillScore.ToString();

    public override void Attached()
    {
        NM.players.Add(entity);

        var evnt = JoinedEvent.Create();
        evnt.Send();

        state.health = 100;
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
        Minimap.SetActive(true);
    }

    public void MyBodySet()
    {
        for (int i = 0; i < MyBody.Length; i++)
            MyBody[i].layer = 0;

        for (int i = 0; i < MyCharacterModelWeapon.Length; i++)
            MyCharacterModelWeapon[i].layer = 0;
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
        if (attacker == nickname.text)
            evnt.isSuicide = true;
        else
            evnt.isSuicide = false;
        evnt.Send();

        if (evnt.isSuicide)
            state.health = 1;
        else
            state.health = 100;

        int RP = Random.Range(0, NM.respawnPoint.Length);
        transform.position = NM.respawnPoint[RP].position;
        transform.rotation = NM.respawnPoint[RP].rotation;
    }

    public void ProgressSub(bool onOff)
    {
        firstPlayerText.gameObject.SetActive(onOff);
        firstScoreText.gameObject.SetActive(onOff);
        myScoreText.gameObject.SetActive(onOff);
    }
}