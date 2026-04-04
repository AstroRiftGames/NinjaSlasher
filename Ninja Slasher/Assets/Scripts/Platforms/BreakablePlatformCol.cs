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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !collision.isTrigger)
        {
            _platform.OnPlayerEnter(collision.gameObject);
        }
    }
}
