using UnityEngine;

public class ConveyorPlatform : PlatformBase
{
    [SerializeField] private float pushSpeed = 2f;
    [SerializeField] private bool pushRight = true;

    private Rigidbody2D playerRb;

    public override void OnPlayerEnter(GameObject player)
    {
        View view = player.GetComponent<View>();
        if (view != null)
            playerRb = view.RB;
    }

    public override void OnPlayerExit(GameObject player)
    {
        playerRb = null;
    }

    protected override void OnPlatformUpdate()
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