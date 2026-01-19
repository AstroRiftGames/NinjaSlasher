using System.Collections;
using UnityEngine;

public enum PlatformTypes
{
    Normal,
    Slippery,
    Elastic,
}
public abstract class PlatformBase : MonoBehaviour, IPlatform
{
    [Header("BASIC SETTINGS")]
    [SerializeField] protected bool isActive = true;
    public PlatformTypes Type => _type;
    [SerializeField] protected PlatformTypes _type = PlatformTypes.Normal; 

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
    public abstract void OnPlayerExit(GameObject player, bool isForced = false);
    public abstract void OnPlatformUpdate();

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
}
