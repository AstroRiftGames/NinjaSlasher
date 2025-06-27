using UnityEngine;

public class SlipperyPlatform : PlatformBase
{
    [SerializeField] private float slideSpeed = 4f;
    [SerializeField] private float falloffVelocity;

    private Rigidbody2D playerRb;
    private Controller playerController;
    private Vector2 slideDirection;
    private bool isSliding = false;

    public override void OnPlayerEnter(GameObject player)
    {
        playerController = player.GetComponent<Controller>();
        if (playerController == null) return;

        View view = player.GetComponent<View>();
        if (view == null) return;

        playerRb = view.RB;
        if (playerRb == null) return;

        Vector2 lastDir = playerController.GetLastDashDirection();
        slideDirection = new Vector2(Mathf.Sign(lastDir.x), 0f);

        playerRb.velocity = Vector2.zero;
        isSliding = true;
    }

    public override void OnPlayerExit(GameObject player)
    {
        isSliding = false;
        if (playerRb != null)
        {
            playerRb.velocityY = -falloffVelocity;
        }
        playerRb = null;
        playerController = null;
    }

    public override void OnPlatformUpdate()
    {
        if (!isSliding || playerRb == null || playerController == null) return;

        if (playerController.IsDashing())
        {
            isSliding = false;
            return;
        }

        playerRb.velocity = slideDirection * slideSpeed;
    }
}
