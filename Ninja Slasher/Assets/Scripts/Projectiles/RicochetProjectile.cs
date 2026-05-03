using UnityEngine;

public class RicochetProjectile : Projectile
{
    [SerializeField] private int _maxBounces;
    [SerializeField] private AudioEvent _ricochetSfx;
    private int _currentBounces;

    public override void Initialize(Vector2 direction, Transform owner)
    {
        base.Initialize(direction, owner);
        _currentBounces = 0;
    }
    public override void OnCollisionEnter2D(Collision2D collision)
    {
        if (TryHandleGameplayClosed())
            return;

        string colTag = collision.gameObject.tag;
        if (colTag is "Scenario" or "Ceiling" or "Floor" or "Obstacle")
        {
            TryRicochet(collision.GetContact(0).normal);
        }
        else
        {
            Collide(collision.collider);
        }
    }

    public void TryRicochet(Vector2 surfaceNormal)
    {
        if (_currentBounces < _maxBounces)
        {
            Ricochet(surfaceNormal);
        }
        else
        {
            _animator.SetTrigger("OnImpact");
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
    }
}
