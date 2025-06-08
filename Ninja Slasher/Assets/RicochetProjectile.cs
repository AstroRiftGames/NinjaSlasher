using Unity.VisualScripting;
using UnityEngine;

public class RicochetProjectile : Projectile
{
    [SerializeField] private int _maxBounces;
    private int _currentBounces;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Scenario"))
        {
            TryRicochet();
        }
        else
        {
            if (!_hasBeenReflected)
            {
                if (IsParryable && _playerInZone != null && _playerInZone.IsParrying())
                {
                    ReflectProjectile(_playerInZone.transform);
                    ResetTime();
                    _currentBounces = 0;
                }
                if (!collision.gameObject.CompareTag("Enemy")) Collide(collision.collider);
            }
            else if (!collision.gameObject.CompareTag("Player"))
            {
                Collide(collision.collider);
            }
        }
    }

    private void TryRicochet()
    {
        if(_currentBounces < _maxBounces)
        {
            Ricochet();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Ricochet()
    {
        _currentBounces++;

    }
}
