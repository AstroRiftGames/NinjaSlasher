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

    [SerializeField] protected Animator _animator;
    public PlatformAudioContext AudioContext => _audioContext;
    protected PlatformAudioContext _audioContext;
    [SerializeField] protected AudioSet _audioSet;

    private void OnEnable()
    {
        CustomUpdateManager.Instance.SubscribeToFixedUpdate(CustomUpdate);
    }
    private void OnDisable()
    {
        CustomUpdateManager.Instance.UnsubscribeFromFixedUpdate(CustomUpdate);

    }

    public virtual void Awake()
    {
        InitializeAudioContext();
    }

    protected virtual void Start()
    {
        InitializePlatform();
    }

    protected virtual void InitializePlatform() { }


    protected virtual void InitializeAudioContext()
    {
        _audioContext = GetComponent<PlatformAudioContext>();
        if (_audioContext != null && _audioSet != null)
            _audioContext.Initialize(_audioSet);
    }

    protected virtual void CustomUpdate()
    {
        if (LevelSessionManager.Instance != null && !LevelSessionManager.Instance.CanProcessGameplay)
            return;

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
