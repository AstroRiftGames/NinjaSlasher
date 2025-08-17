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
    [SerializeField] private Animator _animator;
    [SerializeField] private float _impactTime;

    protected Rigidbody2D _rb;

    protected Controller _playerInZone;
    [SerializeField] protected bool isParryable = true;
    public void SetIsParryable(bool value) => isParryable = value;

    private void OnEnable()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 direction , Transform owner)
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

    public virtual void SetDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        _rb.AddForce(direction* _speed);
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
            string colTag = collision.gameObject.tag;
        if(collision.gameObject.layer == LayerMask.GetMask("Scenario") ||
            collision.gameObject.layer == LayerMask.GetMask("Obstacles") ||
            !IsParryable && colTag is "Player" or "Boss")
        {
            ManageCollision(collision.collider);
        }
    }

    public virtual void ManageCollision(Collider2D collision)
    {
        Debug.Log($"Collided with: {collision.name}");

        Collide(collision);
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

        _animator.SetTrigger("OnImpact");
        _rb.linearVelocity = Vector2.zero;

        Destroy(gameObject, _impactTime);
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
        SetOwner(newShooter);
        _rb.linearVelocity = Vector2.zero;
        SetDirection(newDir);
    }

    public bool IsParryable => isParryable;
}
