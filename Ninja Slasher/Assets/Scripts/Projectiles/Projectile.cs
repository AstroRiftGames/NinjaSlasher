using AstroRift.Core.Pooling;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour, IPoolable
{
    [SerializeField] protected float _speed;
    public float MultiplySpeed(float value) => _speed *= value;
    [SerializeField] protected Transform _shooter;
    public Transform Shooter => _shooter;
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField] protected LayerMask playerLayer;
    [SerializeField] protected LayerMask scenarioLayer;
    [SerializeField] private Animator _animator;
    [SerializeField] private float _impactTime;
    public float ImpactTime => _impactTime;

    protected Rigidbody2D _rb;

    protected Controller _playerInZone;
    [SerializeField] protected bool isParryable = true;
    public void SetIsParryable(bool value) => isParryable = value;

    private bool _isEnhancedParry = false;
    private int _bouncesRemaining = 0;
    private float _velocityRetention = 1f;
    private HashSet<Enemy> _hitEnemies = new HashSet<Enemy>();
    private Collider2D _projectileCollider;

    public event Action<Projectile> OnRequestDespawn;

    public void Initialize(Vector2 direction, Transform owner)
    {
        SetOwner(owner);
        SetDirection(direction);
    }

    public void Initialize(Transform owner)
    {
        SetOwner(owner);
        SetDirection(transform.up);
    }

    public virtual void Update() { }

    public void OnSpawn()
    {
        if (_rb == null) TryGetComponent(out _rb);
        if (_projectileCollider == null) TryGetComponent(out _projectileCollider);
        if (_animator == null) TryGetComponent(out _animator);

        _hitEnemies.Clear();
        _isEnhancedParry = false;
        _bouncesRemaining = 0;
        _velocityRetention = 1f;

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
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(transform.right * _speed);
    }

    private bool IsShooter(Transform collisionTransform)
    {
        if (Shooter == null) return false;

        return collisionTransform == Shooter || collisionTransform.IsChildOf(Shooter) || Shooter.IsChildOf(collisionTransform);
    }

    public virtual void OnCollisionEnter2D(Collision2D collision)
    {
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
                    RequestDespawn();

                return;
            }
        }

        if (Shooter != null && Shooter.tag != colTag &&
            colTag is "Player" or "Boss" or "Enemy" or "Scenario" or "Ceiling" or "Floor" or "Obstacle")
        {
            Collide(collision.collider);
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_isEnhancedParry)
            return;

        if (IsShooter(collision.transform))
            return;

        if (!collision.CompareTag("Enemy"))
            return;

        TryDamageEnemy(collision);
    }

    private void HandleEnhancedParryBounce(Collision2D collision)
    {
        _bouncesRemaining--;

        Vector2 incomingVelocity = _rb.linearVelocity;
        Vector2 normal = collision.contacts[0].normal;
        Vector2 reflectedDirection = Vector2.Reflect(incomingVelocity.normalized, normal);

        _speed *= _velocityRetention;

        _rb.linearVelocity = Vector2.zero;
        SetDirection(reflectedDirection);

        CameraShake.Instance?.TriggerShake(0.1f, 0.15f);
        AudioManager.Instance?.PlaySFXAtPosition(SFXClip.P_ProjectileParried, transform.position);

        if (_bouncesRemaining <= 0)
        {
            EndEnhancedParry();
        }
    }

    public virtual void Collide(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            DamagePlayer(collision.gameObject);
        }
        else if (collision.CompareTag("Enemy"))
        {
            DamageEnemy(collision.gameObject);
        }

        _rb.linearVelocity = Vector2.zero;
        collision.TryGetComponent(out Rigidbody2D rb);
        if (rb != null) rb.linearVelocity = Vector2.zero;
        _animator.SetTrigger("OnImpact");

        RequestDespawn();
    }

    private bool TryDamageEnemy(Collider2D collider)
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
        player.TryGetComponent(out NewController controller);
        controller.Die();
    }

    protected void DamageEnemy(GameObject enemy)
    {
        ParryKillTracker.RegisterParryKill();
        Debug.Log("Parry kill registrada.");

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
        if (Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }

    public virtual void ReflectBackwards(Transform newShooter, Vector2 newDir)
    {
        _animator.SetTrigger("OnParried");
        SetOwner(newShooter);
        _rb.linearVelocity = Vector2.zero;
        SetDirection(newDir);
        SetIsParryable(false);

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

        RequestDespawn();
    }

    public bool IsParryable => isParryable;
}