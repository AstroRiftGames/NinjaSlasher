using UnityEngine;

public abstract class PlatformBase : MonoBehaviour, IPlatform
{
    [Header("BASIC SETTINGS")]
    [SerializeField] protected bool isActive = true;

    protected virtual void Start()
    {
        InitializePlatform();
    }

    protected virtual void InitializePlatform() { }

    protected virtual void Update()
    {
        if (!isActive) return;
        OnPlatformUpdate();
    }

    public abstract void OnPlayerEnter(GameObject player);
    public abstract void OnPlayerExit(GameObject player);
    public abstract void OnPlatformUpdate();

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"{gameObject.name} detected collision wth player");
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
}
