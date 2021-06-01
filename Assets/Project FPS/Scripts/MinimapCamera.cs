using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    [SerializeField] Transform Player;

    private void LateUpdate()
    {
        Vector3 newPos = Player.position;
        newPos.y = transform.position.y;
        transform.position = newPos;

        transform.rotation = Quaternion.Euler(90f, Player.rotation.y, 0f);
    }
}
