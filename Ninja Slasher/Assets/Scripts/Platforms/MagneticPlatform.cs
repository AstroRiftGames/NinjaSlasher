using UnityEngine;

public class MagneticPlatform : PlatformBase
{
    [Header("SETTINGS")]
    [SerializeField] private float attractionRadius;
    [SerializeField] private float attractionForce;
    [SerializeField] private LayerMask playerLayer;

    private void FixedUpdate()
    {
        if (!isActive) return;
        OnPlatformUpdate();
    }

    public override void OnPlatformUpdate()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attractionRadius, playerLayer);

        foreach (var hit in hits)
        {
            NewController playerController = hit.GetComponent<NewController>();
            if (playerController == null || !playerController.IsDashing || playerController.IsParrying)
                continue;

            View playerView = hit.GetComponent<View>();
            if (playerView == null) continue;

            Rigidbody2D rb = playerView.RB;
            if (rb == null) continue;

            Vector2 direction = ((Vector2)transform.position - rb.position).normalized;
            rb.AddForce(direction * attractionForce, ForceMode2D.Force);

            Debug.DrawRay(rb.position, direction * 2f, Color.red);
        }
    }

    public override void OnPlayerEnter(GameObject player) { }
    public override void OnPlayerExit(GameObject player) { }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attractionRadius);
    }
}
