using UnityEngine;

public abstract class PlatformBase : MonoBehaviour
{
    [Header("BASIC SETTINGS")]
    [SerializeField] protected bool isActive = true;

    protected virtual void Start()
    {
        InitializePlatform();
    }

    protected virtual void Update()
    {
        if (!isActive) return;
        OnPlatformUpdate();
    }

    protected virtual void InitializePlatform() { }

    protected virtual void OnPlatformUpdate() { }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            OnPlayerEnter(collision.gameObject);
        }
    }

    protected virtual void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            OnPlayerExit(collision.gameObject);
        }
    }

    public abstract void OnPlayerEnter(GameObject player);

    public abstract void OnPlayerExit(GameObject player);
}
