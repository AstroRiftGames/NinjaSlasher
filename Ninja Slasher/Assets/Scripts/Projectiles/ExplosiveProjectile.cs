using UnityEngine;

public class ExplosiveProjectile : Projectile
{
    [SerializeField] private float _explosionRange;

    public override void Collide(Collider2D collision)
    {
        Collider2D playerCol = Physics2D.OverlapCircle(
            transform.position,
            _explosionRange,
            playerLayer
        );

        if (playerCol != null)
        {
            DamagePlayer(playerCol.gameObject);
        }

        base.Collide(collision);
    }

    public override void SetDirection(Vector2 direction)
    {
        base.SetDirection(direction);
        Debug.Log("Dir set");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _explosionRange);
    }
#endif
}
