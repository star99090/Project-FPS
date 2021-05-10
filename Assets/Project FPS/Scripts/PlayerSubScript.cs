using UnityEngine;
using UnityEngine.UI;
using static NetworkManager;
#pragma warning disable CS0618

public class PlayerSubScript : Bolt.EntityBehaviour<IFPSPlayerState>
{
    public GameObject[] HideObjects;
    public GameObject NicknameCanvas;
    public GameObject MyBody;
    public Text nickname;
    public Text firstPlayerText;
    public Text firstScoreText;
    public Text myScoreText;
    
    public Transform arm;
    public Text HealthText;
    public int myKillScore = 0;

    string attacker;
    BoltEntity attackerEntity;

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

    public void NicknameSet() => NicknameCanvas.SetActive(false);
    public void MyBodySet() => MyBody.SetActive(true);

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
        evnt.Send();

        state.health = 100;
        transform.position = new Vector3(Random.Range(-5, 5), 0, 0);
        transform.rotation = Quaternion.EulerAngles(Vector3.zero);
    }

    public void UpdateMyScore()
    {
        myScoreText.text = "MY SCORE " + myKillScore;
    }
}