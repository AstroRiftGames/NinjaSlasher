using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private Portal connectedPortal;
    [SerializeField] private bool keepDirection;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        PlayerView view = collision.GetComponent<PlayerView>();
        if (view == null || connectedPortal == null) return;

        if (view.LastUsedPortal == this) return;

        Vector2 entryVelocity = view.RB.linearVelocity;

        view.transform.position = connectedPortal.transform.position;
        if (keepDirection)
            view.RB.linearVelocity = entryVelocity;

        view.LastUsedPortal = connectedPortal;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerView view = collision.GetComponent<PlayerView>();
            if (view != null && view.LastUsedPortal == this)
                view.LastUsedPortal = null;
        }
    }
}
