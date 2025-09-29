using UnityEngine;

public class BreakablePlatformCol : MonoBehaviour
{
    [SerializeField] BreakablePlatform _platform;
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            _platform.OnPlayerEnter(collision.gameObject);
        }
    }

    protected virtual void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            _platform.OnPlayerExit(collision.gameObject);
        }
    }
}
