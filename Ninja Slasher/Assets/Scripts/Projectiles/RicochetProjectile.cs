using UnityEngine;

public class RicochetProjectile : Projectile
{
    [SerializeField] private int _maxBounces;
    [SerializeField] private AudioEvent _ricochetSfx;
    private int _currentBounces;

    public override void Initialize(Vector2 direction, Transform owner, bool isParryable)
    {
        base.Initialize(direction, owner, isParryable);
        _currentBounces = 0;
    }

    public override void OnCollisionEnter2D(Collision2D collision)
    {
        if (TryHandleGameplayClosed())
            return;

        string colTag = collision.gameObject.tag;
        if (colTag is "Scenario" or "Ceiling" or "Floor" or "Obstacle")
        {
            TryRicochet(collision);
        }
        else
        {
            Collide(collision.collider);
        }
    }

    public void TryRicochet(Collision2D collision)
    {
        if (_currentBounces < _maxBounces)
        {
            Ricochet(collision.GetContact(0).normal);
        }
        else
        {
            Collide(collision.collider);
        }
    }
    private void Ricochet(Vector2 surfaceNormal)
    {
        _currentBounces++;

        Vector2 newDir = Vector2.Reflect(CurrentDir, surfaceNormal);
        SetDirection(newDir);

        if (_ricochetSfx != null)
        {
            AudioService.Instance?.PlaySFXAtPosition(_ricochetSfx, transform.position);
        }
        if (_particleSystem != null)
        {
            _particleSystem.Play();
        }
    }
}
