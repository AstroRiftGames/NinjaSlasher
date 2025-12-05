using Unity.VisualScripting;
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
            TryRicochet();
        }
        else
        {
            Collide(collision.collider);
        }
    }

    private void TryRicochet()
    {
        if (_currentBounces < _maxBounces)
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
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.Proj_Ricochet_Bounce, transform.position);
    }
}
