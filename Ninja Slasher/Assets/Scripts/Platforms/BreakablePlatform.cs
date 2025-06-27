using UnityEngine;

public class BreakablePlatform : PlatformBase
{
    [Header("SETTINGS")]
    [SerializeField] private int maxUses;
    [SerializeField] private float falloffVelocity;

    private Rigidbody2D playerRb;
    private int remainingUses;

    protected override void InitializePlatform()
    {
        remainingUses = maxUses;
    }

    public override void OnPlayerEnter(GameObject player)
    {
        View view = player.GetComponent<View>();
        if (view != null)
            playerRb = view.RB;
        if (!isActive) return;

        playerRb.velocity = Vector2.zero;

        remainingUses--;

        if (remainingUses <= 0)
        {
            Break();
        }
    }

    public override void OnPlayerExit(GameObject player) { }

    public override void OnPlatformUpdate() { }

    private void Break()
    {
        isActive = false;

        if (playerRb != null)
        {
            Debug.Log("[BREAK] Antes - Velocidad: " + playerRb.velocity + " Gravedad: " + playerRb.gravityScale);

            playerRb.velocity = new Vector2(playerRb.velocity.x, -Mathf.Abs(falloffVelocity));

            Debug.Log("[BREAK] Después - Velocidad: " + playerRb.velocity);

            var controller = playerRb.GetComponent<Controller>();
            if (controller != null)
                controller.ForceExitSurface();
        }

        StartCoroutine(DestroyNextFrame());
    }

    private System.Collections.IEnumerator DestroyNextFrame()
    {
        yield return null;
        Destroy(gameObject);
    }

}
