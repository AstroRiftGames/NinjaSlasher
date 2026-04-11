using UnityEngine;

public class BreakablePlatformCol : MonoBehaviour
{
    [SerializeField] BreakablePlatform _platform;

    private bool IsValidInteraction(GameObject obj, bool isTriggerHit)
    {
        if (obj.CompareTag("Player") || obj.layer == LayerMask.NameToLayer("Ball"))
        {
            return !isTriggerHit;
        }
        else if (obj.CompareTag("Projectile"))
        {
            return true;
        }
        return false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsValidInteraction(collision.gameObject, false))
        {
            _platform.OnPlayerEnter(collision.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsValidInteraction(collision.gameObject, collision.isTrigger))
        {
            _platform.OnPlayerEnter(collision.gameObject);
        }
    }
}
