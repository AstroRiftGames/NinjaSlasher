using UnityEngine;

public class BreakablePlatform : PlatformBase
{
    [Header("SETTINGS")]
    [SerializeField] private int maxUses;

    [SerializeField] private float falloffVelocity;
    Rigidbody2D playerRb;
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

        playerRb.linearVelocity = Vector2.zero;

        remainingUses--;

        if (remainingUses <= 0)
        {
            Break();
        }
    }

    public override void OnPlayerExit(GameObject player)
    {

    }

    private void Break()
    {
        isActive = false;
        playerRb.linearVelocityY = -falloffVelocity;
        Destroy(gameObject);
    }
}
