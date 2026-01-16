using UnityEngine;
using System.Collections;

public class ElasticPlatform : PlatformBase
{
    [SerializeField] private float bounceForce = 15f;
    [SerializeField] private bool isVerticalWall = false;

    public override void OnPlayerEnter(GameObject player)
    {
        NewController controller = player.GetComponent<NewController>();
        if (controller == null)
        {
            return;
        }

        Vector2 lastDashDir = controller.LastDashDirection;

        if (lastDashDir == Vector2.zero)
            return;
        

        View view = player.GetComponent<View>();
        if (view == null)
            return;

        Rigidbody2D rb = view.RB;
        if (rb == null)        
            return;        

        StartCoroutine(ApplyBounceAfterCollision(rb, lastDashDir.normalized, controller));
    }

    private IEnumerator ApplyBounceAfterCollision(Rigidbody2D rb, Vector2 dashDir, NewController controller)
    {
        yield return new WaitForFixedUpdate();

        Vector2 bounceDir;

        if (isVerticalWall)
        {
            bounceDir = new Vector2(-dashDir.x, dashDir.y);
        }
        else
        {
            Vector2 surfaceNormal = Vector2.up;
            bounceDir = dashDir - 2 * Vector2.Dot(dashDir, surfaceNormal) * surfaceNormal;

            if (bounceDir.y < 0)
            {
                bounceDir.y = -bounceDir.y;
            }
        }

        bounceDir.Normalize();

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(bounceDir * bounceForce, ForceMode2D.Impulse);

        //controller.ForceExitSurface();

        Debug.DrawRay(rb.position, dashDir * 2f, Color.red, 2f);
        Debug.DrawRay(rb.position, bounceDir * 2f, Color.green, 2f);
    }

    public override void OnPlayerExit(GameObject player) { }

    public override void OnPlatformUpdate() { }

    private void OnDrawGizmos()
    {
        Gizmos.color = isVerticalWall ? Color.red : Color.blue;
        Vector3 size = GetComponent<Collider2D>()?.bounds.size ?? Vector3.one;
        Gizmos.DrawWireCube(transform.position, size);

        if (isVerticalWall)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, Vector3.left * 1f);
            Gizmos.DrawRay(transform.position, Vector3.right * 1f);
        }
        else
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, Vector3.up * 1f);
        }
    }
}