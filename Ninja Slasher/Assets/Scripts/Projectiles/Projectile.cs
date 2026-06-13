using System;
using UnityEngine;

public interface IParryOverrideProvider
{
    bool ForcesAllProjectilesParryable { get; }
    event Action<bool> ParryOverrideChanged;
}

public class Projectile : MonoBehaviour, IPoolable
{
    [SerializeField] private ProjectileAudioSet _audioSet;
    protected ProjectileAudioContext _audioContext;
    public ProjectileAudioContext AudioContext => _audioContext;

    [SerializeField] protected float _speed;
    public float MultiplySpeed(float value) => _speed *= value;
    [SerializeField] protected Transform _shooter;
    public Transform Shooter => _shooter;
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField] protected LayerMask playerLayer;
    [SerializeField] protected LayerMask scenarioLayer;
    [SerializeField] protected Animator _animator;
    [SerializeField] protected Collider2D _col;
    [SerializeField] protected SpriteRenderer[] _renderers;
    [SerializeField] protected ParticleSystem _particleSystem;
    [SerializeField] protected Color _projColor;
    [SerializeField] private Color _parryableColor = Color.blue;
    [SerializeField] private Color _nonParryableColor = Color.red;

    protected Rigidbody2D _rb;

    public bool BaseParryable => isParryable;
    public bool EffectiveParryable =>
        isParryable || (_parryOverrideProvider?.ForcesAllProjectilesParryable ?? false);
    public bool IsParryable => EffectiveParryable;
    [SerializeField] protected bool isParryable = true;
    public void SetBaseParryable(bool newValue)
    {
        isParryable = newValue;
        RefreshParryableVisuals();
    }

    public void SetIsParryable(bool newValue)
    {
        SetBaseParryable(newValue);
    }

    private IParryOverrideProvider _parryOverrideProvider;
    protected IParryOverrideProvider ParryOverrideProvider => _parryOverrideProvider;

    public void SetParryOverrideProvider(IParryOverrideProvider provider)
    {
        UnsubscribeFromParryOverride();
        _parryOverrideProvider = provider;
        SubscribeToParryOverride();
        RefreshParryableVisuals();
    }

    private void RefreshParryableVisuals()
    {
        bool isEffectivelyParryable = EffectiveParryable;

        if (_particleSystem != null)
        {
            var main = _particleSystem.main;
            main.startColor = isEffectivelyParryable ? _parryableColor : _nonParryableColor;
        }

        if (_renderers == null)
            return;

        foreach (var renderer in _renderers)
        {
            if (renderer != null)
                renderer.color = isEffectivelyParryable ? _parryableColor : _nonParryableColor;
        }
    }

    private void SubscribeToParryOverride()
    {
        if (_parryOverrideProvider == null || !isActiveAndEnabled)
            return;

        _parryOverrideProvider.ParryOverrideChanged -= HandleParryOverrideChanged;
        _parryOverrideProvider.ParryOverrideChanged += HandleParryOverrideChanged;
    }

    private void UnsubscribeFromParryOverride()
    {
        if (_parryOverrideProvider != null)
            _parryOverrideProvider.ParryOverrideChanged -= HandleParryOverrideChanged;
    }

    private void HandleParryOverrideChanged(bool _)
    {
        RefreshParryableVisuals();
    }

    private Vector2 _currentDir;
    public Vector2 CurrentDir => _currentDir;

    public bool WasReflected { get; private set; }

    public event Action<Projectile> OnRequestDespawn;

    public virtual void Initialize(Vector2 direction, Transform owner, bool isParryable = false)
    {
        SetBaseParryable(isParryable);
        InitializeAudioContext();
        SetOwner(owner);
        SetDirection(direction);
        transform.parent = null;
        transform.localScale = Vector3.one/2;
    }

    public void Initialize(Transform owner)
    {
        InitializeAudioContext();
        SetOwner(owner);
        SetDirection(transform.up);
        transform.parent = null;
        transform.localScale = Vector3.one/2;
    }

    public virtual void Update()
    {
        if (TryHandleGameplayClosed())
            return;
    }

    protected virtual void OnEnable()
    {
        SubscribeToParryOverride();
        RefreshParryableVisuals();
    }

    public void OnSpawn()
    {
        if (_rb == null) TryGetComponent(out _rb);
        if (_animator == null) TryGetComponent(out _animator);
        if(_col == null) TryGetComponent(out _col); 
        if (_renderers == null)
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        WasReflected = false;

        _rb.linearVelocity = Vector2.zero;
        SubscribeToParryOverride();
        RefreshParryableVisuals();
    }

    public void OnDespawn()
    {
        UnsubscribeFromParryOverride();
        StopAllCoroutines();
        _rb.linearVelocity = Vector2.zero;
    }

    protected virtual void OnDisable()
    {
        UnsubscribeFromParryOverride();
    }

    public void RequestDespawn()
    {
        OnRequestDespawn?.Invoke(this);
    }

    public virtual void SetDirection(Vector2 direction)
    {
        _rb.linearVelocity = Vector2.zero;

        _currentDir = direction;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        _rb.AddForce(transform.right * _speed);
    }

    protected bool IsShooter(Transform collisionTransform)
    {
        if (Shooter == null) return false;

        return collisionTransform == Shooter || collisionTransform.IsChildOf(Shooter) || Shooter.IsChildOf(collisionTransform);
    }

    public virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (TryHandleGameplayClosed())
            return;

        string colTag = collision.gameObject.tag;

        if (IsShooter(collision.transform))
            return;

        if (Shooter != null && Shooter.tag != colTag)
        {
            Collide(collision.collider);
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (TryHandleGameplayClosed())
            return;

        if (IsShooter(collision.transform))
            return;

        if (!collision.CompareTag("Enemy"))
            return;

        Collide(collision);
    }

    public virtual void Collide(Collider2D collision)
    {
        if (TryHandleGameplayClosed())
            return;

        if (collision.CompareTag("Player"))
        {
            DamagePlayer(collision.gameObject);
        }
        else if (collision.CompareTag("Enemy"))
        {
            DamageEnemy(collision.gameObject);
        }

        _rb.linearVelocity = Vector2.zero;
        _animator.SetTrigger("OnImpact");
        _particleSystem.Play();
        _col.enabled = false;
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.impact, transform.position);
        _rb.linearVelocity = Vector2.zero;
    }

    protected void DamagePlayer(GameObject player)
    {
        if (LevelSessionManager.Instance != null && !LevelSessionManager.Instance.CanProcessGameplay)
            return;

        player.TryGetComponent(out PlayerController controller);
        controller.Die();
    }

    protected void DamageEnemy(GameObject enemy)
    {
        if (LevelSessionManager.Instance != null && !LevelSessionManager.Instance.CanProcessGameplay)
            return;

        if (WasReflected)
        {
            ParryKillTracker.RegisterParryKill();
        }

        enemy.TryGetComponent(out Enemy script);

        script.Die();
    }

    public void SetOwner(Transform shooter)
    {
        _shooter = shooter;
        gameObject.layer = _shooter.tag == "Player" ? 12 : 8;
    }

    protected void ResetTime()
    {
        if (Time.fixedDeltaTime != 0.02f)
            Time.fixedDeltaTime = 0.02f;
    }

    public virtual void ReflectBackwards(Transform newShooter, Vector2 newDir)
    {
        if (TryHandleGameplayClosed())
            return;

        WasReflected = true;
        _animator.SetTrigger("OnParried");
        SetOwner(newShooter);
        _rb.linearVelocity = Vector2.zero;
        SetDirection(newDir);
    }

    protected virtual void InitializeAudioContext()
    {
        _audioContext = GetComponent<ProjectileAudioContext>();
        if (_audioContext != null)
            _audioContext.Initialize(_audioSet);
    }

    protected bool TryHandleGameplayClosed()
    {
        if (LevelSessionManager.Instance == null || LevelSessionManager.Instance.CanProcessGameplay)
            return false;

        HandleGameplayClosed();
        return true;
    }

    protected void HandleGameplayClosed()
    {
        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;

        if (OnRequestDespawn != null)
            OnRequestDespawn.Invoke(this);
        else
            Destroy(gameObject);
    }
}
