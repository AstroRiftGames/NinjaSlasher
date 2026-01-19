using UnityEngine;

public class ShootingPointContainer : MonoBehaviour
{
    private Transform _player;

    public void SetPlayer(Transform player)
    {
        _player = player;
    }

    private void Update()
    {
        if (_player == null)
            return;

        Vector2 dir = (Vector2)transform.position - (Vector2)_player.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + 90);
    }
}
