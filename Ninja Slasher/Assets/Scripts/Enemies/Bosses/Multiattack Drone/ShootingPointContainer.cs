using UnityEngine;

public class ShootingPointContainer : MonoBehaviour
{
    private Transform _player;

    private void Awake()
    {
        _player = FindAnyObjectByType<Controller>().transform;
    }

    private void Update()
    {
        Vector2 dir = transform.position - _player.position;
        float angle = (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        transform.rotation = Quaternion.Euler(0, 0, angle + 90);
    }
}
