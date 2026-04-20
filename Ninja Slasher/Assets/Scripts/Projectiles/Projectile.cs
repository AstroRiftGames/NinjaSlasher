using System;
using System.Collections.Generic;
using UnityEngine;

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

    protected Rigidbody2D _rb;

    public bool IsParryable => isParryable;
    [SerializeField] protected bool isParryable = true;
    public void SetIsParryable(bool newValue)
    {
        isParryable = newValue;
        Debug.Log("Proj parryable updated. New value: " + newValue);
    }

    protected bool _isEnhancedParry = false;
    protected int _bouncesRemaining = 0;
    private float _velocityRetention = 1f;
    private HashSet<Enemy> _hitEnemies = new HashSet<Enemy>();
    private Collider2D _projectileCollider;
    private Vector2 _currentDir;
    public Vector2 CurrentDir => _currentDir;

    public bool WasReflected { get; private set; }

    public event Action<Projectile> OnRequestDespawn;

    public void Initialize(Vector2 direction, Transform owner)
    {
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

    public void OnSpawn()
    {
        if (_rb == null) TryGetComponent(out _rb);
        if (_projectileCollider == null) TryGetComponent(out _projectileCollider);
        if (_animator == null) TryGetComponent(out _animator);

        _hitEnemies.Clear();
        _isEnhancedParry = false;
        _bouncesRemaining = 0;
        _velocityRetention = 1f;
        WasReflected = false;

        _rb.linearVelocity = Vector2.zero;
    }

    public void OnDespawn()
    {
        StopAllCoroutines();
        _rb.linearVelocity = Vector2.zero;
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

        if (_isEnhancedParry)
        {
            if (colTag == "Enemy")
            {
                TryDamageEnemy(collision.collider);
                return;
            }

            if (colTag is "Scenario" or "Ceiling" or "Floor" or "Obstacle")
            {
                if (_bouncesRemaining > 0)
                    HandleEnhancedParryBounce(collision);
                else
                    Collide(collision.collider);

                return;
            }
        }

        if (Shooter != null && Shooter.tag != colTag)
        {
            Collide(collision.collider);
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (TryHandleGameplayClosed())
            return;

        //if (!_isEnhancedParry)
        //    return;

        if (IsShooter(collision.transform))
            return;

        if (!collision.CompareTag("Enemy"))
            return;

        Collide(collision);
    }

    protected void HandleEnhancedParryBounce(Collision2D collision)
    {
        _bouncesRemaining--;

        Vector2 incomingVelocity = _rb.linearVelocity;
        Vector2 normal = collision.contacts[0].normal;
        Vector2 reflectedDirection = Vector2.Reflect(incomingVelocity.normalized, normal);

        _speed *= _velocityRetention;

        _rb.linearVelocity = Vector2.zero;
        SetDirection(reflectedDirection);

        CameraShake.Instance?.TriggerShake(0.1f, 0.15f);

        if (_bouncesRemaining <= 0)
        {
            EndEnhancedParry();
        }
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

        _animator.SetTrigger("OnImpact");
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.impact, transform.position);
        _rb.linearVelocity = Vector2.zero;
    }

    protected bool TryDamageEnemy(Collider2D collider)
    {   
        Enemy enemy = collider.GetComponentInParent<Enemy>()
                      ?? collider.GetComponent<Enemy>();

        if (enemy == null)
            return false;

        if (_hitEnemies.Contains(enemy))
            return false;

        _hitEnemies.Add(enemy);

        Physics2D.IgnoreCollision(_projectileCollider, collider, true);

        DamageEnemy(enemy.gameObject);
        return true;
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

        var context = PowerUpManager.Instance?.context;
        if (context != null && context.EnhancedParryActive)
        {
            EnableEnhancedParry(context.EnhancedParryBounces, context.EnhancedParryVelocityRetention);
        }
    }

    public void EnableEnhancedParry(int bounces, float velocityRetention)
    {
        _isEnhancedParry = true;
        _bouncesRemaining = bounces;
        _velocityRetention = velocityRetention;
        _hitEnemies.Clear();

        transform.SetParent(null);
    }

    private void EndEnhancedParry()
    {
        _isEnhancedParry = false;

        _animator.SetTrigger("OnImpact");
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.impact, transform.position);
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
