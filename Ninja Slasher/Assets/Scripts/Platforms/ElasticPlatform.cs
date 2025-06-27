using UnityEngine;
using System.Collections;

public class ElasticPlatform : PlatformBase
{
    [SerializeField] private float bounceForce = 15f;

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

        StartCoroutine(ApplyBounceAfterCollision(rb, dashDir, controller));
    }

    private IEnumerator ApplyBounceAfterCollision(Rigidbody2D rb, Vector2 dashDir, Controller controller)
    {
        yield return new WaitForFixedUpdate();

        Vector2 surfaceNormal = Vector2.up;

        Vector2 bounceDir = dashDir - 2 * Vector2.Dot(dashDir, surfaceNormal) * surfaceNormal;

        if (bounceDir.y < 0)
        {
            bounceDir.y = -bounceDir.y;
        }

        bounceDir.Normalize();

        rb.velocity = Vector2.zero;
        rb.AddForce(bounceDir * bounceForce, ForceMode2D.Impulse);

        controller.ForceExitSurface();

        Debug.Log($"ElasticPlatform: Entrada {dashDir}, Rebote {bounceDir}");
        Debug.DrawRay(rb.position, dashDir * 2f, Color.red, 2f);
        Debug.DrawRay(rb.position, bounceDir * 2f, Color.green, 2f);
    }

    public override void OnPlayerExit(GameObject player) { }
    public override void OnPlatformUpdate() { }
}