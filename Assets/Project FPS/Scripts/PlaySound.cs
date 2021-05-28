using System.Collections;
using UnityEngine;

public class PlaySound : MonoBehaviour
{
    private void Start()
    {
        if (GetComponent<BoltEntity>().IsOwner)
            GetComponent<AudioSource>().volume = 0f;

        StartCoroutine(DestroySelf());
    }

    IEnumerator DestroySelf()
    {
        yield return new WaitForSeconds(1.8f);

        if (GetComponent<BoltEntity>().IsOwner)
            BoltNetwork.Destroy(gameObject);
    }
}
