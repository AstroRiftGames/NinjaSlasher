using UnityEngine;

public class ConveyorPlatform : PlatformBase
{
    [SerializeField] private float pushSpeed = 2f;
    [SerializeField] private bool pushRight = true;
    [SerializeField] private float falloffVelocity = 5f;

    private Rigidbody2D playerRb;
    private Controller playerController;

    public override void OnPlayerEnter(GameObject player)
    {
        View view = player.GetComponent<View>();
        if (view != null)
            playerRb = view.RB;

        playerController = player.GetComponent<Controller>();
    }

    public override void OnPlayerExit(GameObject player)
    {
        if (player.GetComponent<View>()?.RB == playerRb)
        {
            if (playerRb != null)
            {
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, -falloffVelocity);
            }

            playerRb = null;
            playerController = null;
        }
    }

    public override void OnPlatformUpdate()
    {
        if (!isActive || playerRb == null) return;

        float direction = pushRight ? 1f : -1f;
        Vector2 velocity = playerRb.linearVelocity;
        velocity.x = direction * pushSpeed;
        playerRb.linearVelocity = velocity;
    }

    public void ToggleDirection()
    {
        pushRight = !pushRight;
    }
}