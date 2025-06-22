using UnityEngine;

public class ElasticPlatform : PlatformBase
{
    [SerializeField] private float bounceForce;

    public override void OnPlayerEnter(GameObject player)
    {
        Controller controller = player.GetComponent<Controller>();
        if (controller == null || !controller.IsDashing()) return;

        View view = player.GetComponent<View>();
        if (view == null) return;

        Rigidbody2D rb = view.RB;
        if (rb == null) return;

        Vector2 dashDir = controller.GetDashDirection().normalized;
        if (dashDir == Vector2.zero) return;

        Vector2 bounceDir;

        if (Mathf.Abs(dashDir.x) > Mathf.Abs(dashDir.y))
        {
            bounceDir = new Vector2(dashDir.x, -Mathf.Sign(dashDir.x));
        }
        else
        {
            bounceDir = new Vector2(-Mathf.Sign(dashDir.y), dashDir.y);
        }

        bounceDir.Normalize();

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(bounceDir * bounceForce, ForceMode2D.Impulse);

        Debug.Log($"ElasticPlatform: Entrada {dashDir}, Rebote {bounceDir}");
        Debug.DrawRay(rb.position, bounceDir * 2f, Color.magenta, 1f);
    }

    public override void OnPlayerExit(GameObject player) { }

    public override void OnPlatformUpdate() { }
}
