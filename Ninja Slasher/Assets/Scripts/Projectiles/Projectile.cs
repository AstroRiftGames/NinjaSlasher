using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Projectile : MonoBehaviour
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
    GenericPool<Projectile> _ownerPool;
    public GenericPool<Projectile> OwnerPool => _ownerPool;
    private void SetPool(GenericPool<Projectile> ownerPool) => _ownerPool = ownerPool;

    private void OnEnable()
    {
        TryGetComponent(out Rigidbody2D rb);
        _rb = rb;
        TryGetComponent(out Animator animator);
        _animator = animator;
        if(_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }
    }

    public void Initialize(Vector2 direction, Transform owner, GenericPool<Projectile> pool = null)
    {
        if(pool != null) SetPool(pool);
        SetOwner(owner);
        SetDirection(direction);
    }

    public void Initialize(Transform owner, GenericPool<Projectile> pool = null)
    {
        if (pool != null) SetPool(pool);
        SetOwner(owner);
        SetDirection(transform.up);
    }

    public virtual void Update() { }

    public virtual void SetDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(transform.right * _speed);
    }

    public virtual void OnCollisionEnter2D(Collision2D collision)
    {
        string colTag = collision.gameObject.tag;
        if (Shooter.tag != colTag && colTag is "Player" or "Boss" or "Scenario" or "Ceiling" or "Floor" or "Enemy" or "Obstacle")
        {
            Collide(collision.collider);
        }
    }

    public virtual void Collide(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            DamagePlayer(collision.gameObject);
        }
        else if (collision.CompareTag("Enemy"))
        {
            DamageEnemy(collision.gameObject);
        }

        if (_ownerPool == null)
        {
            Destroy(gameObject, _impactTime);
        }
        else
        {
            StartCoroutine(ReturnProjectile());
        }
        _rb.linearVelocity = Vector2.zero;
        collision.TryGetComponent(out Rigidbody2D rb);
        rb.linearVelocity = Vector2.zero;
        _animator.SetTrigger("OnImpact");
    }

    IEnumerator ReturnProjectile()
    {
        yield return new WaitForSeconds(_impactTime);
        _ownerPool.Return(this);
    }

    protected void DamagePlayer(GameObject player)
    {
        player.TryGetComponent(out Controller controller);
        controller.Die();
    }

    protected void DamageEnemy(GameObject enemy)
    {
        ParryKillTracker.RegisterParryKill();
        Debug.Log("Parry kill registrada.");

        EnemyTracker tracker = FindObjectOfType<EnemyTracker>();

        enemy.TryGetComponent(out Enemy script);

        if (tracker != null)
        {
            tracker.OnEnemyKilled(script);
        }
        script.Die();
    }

    public void SetOwner(Transform shooter)
    {
        _shooter = shooter;
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
    }

    public bool IsParryable => isParryable;
}
