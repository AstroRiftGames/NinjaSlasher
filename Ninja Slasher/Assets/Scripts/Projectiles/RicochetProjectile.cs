using UnityEngine;

public class RicochetProjectile : Projectile
{
    [SerializeField] private int _maxBounces;
    private int _currentBounces;

    public override void OnCollisionEnter2D(Collision2D collision)
    {
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

        //TODO: Use AudioService instead of AudioManager and add ricochet sound
        //AudioManager.Instance.PlaySFXAtPosition(SFXClip.Proj_Ricochet_Bounce, transform.position);
    }
}
