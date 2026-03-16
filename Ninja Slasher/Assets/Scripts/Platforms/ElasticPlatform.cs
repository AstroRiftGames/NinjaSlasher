using System.Collections;
using UnityEngine;

public class ElasticPlatform : PlatformBase
{
    [SerializeField] private float bounceForce = 15f;
    [SerializeField] private bool isVerticalWall = false;

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            OnPlayerEnter(collision.gameObject, collision.GetContact(0));
        }
    }

    public override void OnPlayerEnter(GameObject player) { }
    public void OnPlayerEnter(GameObject player, ContactPoint2D contactPoint)
    {
        NewController controller = player.GetComponent<NewController>();
        if (controller == null) return;     

        View view = controller.View;
        if (view == null) return;

        Rigidbody2D rb = view.RB;
        if (rb == null) return;

        //AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.Interaction, player.transform.position);

        if (_animator != null && _animator.enabled)
        {
            _animator.SetTrigger("OnBounce");
        }

        controller.ForceDash(GetReflectedDir(controller.LastMoveDirection, contactPoint));
    }

    private Vector2 GetReflectedDir(Vector2 originalDir, ContactPoint2D contactPoint)
    {
        return Vector2.Reflect(originalDir, contactPoint.normal).normalized;
    }

    public override void OnPlayerExit(GameObject player, bool isForced = false) { }

    public override void OnPlatformUpdate() { }
}