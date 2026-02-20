using System.Collections;
using UnityEngine;

public enum PlatformTypes
{
    Normal,
    Slippery,
    Elastic,
    Breakable,
    Geyser,
    Magnetic,
}
public abstract class PlatformBase : MonoBehaviour, IPlatform
{
    [Header("BASIC SETTINGS")]
    [SerializeField] protected bool isActive = true;
    public PlatformTypes Type => _type;
    [SerializeField] protected PlatformTypes _type = PlatformTypes.Normal;

    public SFXClip Clip => _clip;
    private SFXClip _clip = SFXClip.P_Landing_General;
    [SerializeField] protected Animator _animator;

    private void OnEnable()
    {
        CustomUpdateManager.Instance.SubscribeToUpdate(CustomUpdate);
    }
    private void OnDisable()
    {
        CustomUpdateManager.Instance.UnsubscribeFromUpdate(CustomUpdate);

    }

    protected virtual void Start()
    {
        InitializePlatform();
        _clip = _type switch
        {
            PlatformTypes.Normal => SFXClip.P_Landing_General,
            PlatformTypes.Elastic => SFXClip.P_Landing_Elastic,
            PlatformTypes.Slippery => SFXClip.P_Landing_Slippery,
            PlatformTypes.Breakable => SFXClip.P_Landing_Breakable,
            _ => _clip,
        };
    }

    protected virtual void InitializePlatform() { }

    protected virtual void CustomUpdate()
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
