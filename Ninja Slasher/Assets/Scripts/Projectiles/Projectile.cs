using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Projectile : MonoBehaviour
{
    [SerializeField] protected float _speed;
    [SerializeField] private float _reflectedSpeed;
    [SerializeField] protected Transform _shooter;
    public Transform Shooter => _shooter;
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField] protected LayerMask playerLayer;

    protected Rigidbody2D _rb;
    protected bool _hasBeenReflected = false;

    private bool _timeSlowed = false;
    protected Controller _playerInZone;
    [SerializeField] protected bool isParryable = true;

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

    public virtual void Update()
    {

    }

    public virtual void SetDirection(Vector2 direction)
    {
        _rb.AddForce(direction* _speed);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("ParryZone"))
        {
            _playerInZone = collision.GetComponentInParent<Controller>();
            if (_playerInZone == null) return;

            if (IsParryable && _playerInZone.IsParrying())
            {
                ReflectProjectile(_playerInZone.transform);
            }
            else if (!_timeSlowed)
            {
                StartCoroutine(SlowTimeUntilParry());
            }
        }
        else
        {
            if (!_timeSlowed) ManageCollision(collision);
        }
    }

    public virtual void ManageCollision(Collider2D collision)
    {
        if (!_hasBeenReflected)
        {
            if (_timeSlowed) return;

            if (IsParryable && _playerInZone != null && _playerInZone.IsParrying())
            {
                ReflectProjectile(_playerInZone.transform);
                ResetTime();
            }
            else if (!collision.CompareTag("Enemy"))
            {
                Collide(collision);
            }
        }
        else if (!collision.CompareTag("Player"))
        {
            Collide(collision);
        }
    }

    protected void ReflectProjectile(Transform playerTransform)
    {
        if (_hasBeenReflected) return;

        Transform target = _shooter != null ? _shooter : FindClosestEnemy();
        if (target == null)
        {
            return;
        }

        Vector2 direction = (target.position - transform.position).normalized;
        _rb.linearVelocity = direction * _reflectedSpeed;
        _hasBeenReflected = true;

        transform.right = direction;

        Debug.Log("Proyectil reflejado hacia: " + target.name);
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

        Destroy(gameObject);
    }

    protected void DamagePlayer(GameObject player)
    {
        player.TryGetComponent(out Controller controller);
        controller.Die();
    }

    protected void DamageEnemy(GameObject enemy)
    {
        if (_hasBeenReflected)
        {
            ParryKillTracker.RegisterParryKill();
            Debug.Log("Parry kill registrada.");
        }

        EnemyTracker tracker = FindObjectOfType<EnemyTracker>();

        enemy.TryGetComponent(out Enemy script);
        Enemy enemyScript = script;

        if (tracker != null)
        {
            tracker.OnEnemyKilled(enemyScript);
        }
        enemyScript.Die();
    }

    public void SetOwner(Transform shooter)
    {
        _shooter = shooter;
    }

    private IEnumerator SlowTimeUntilParry()
    {
        _timeSlowed = true;
        Time.timeScale = 0.3f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        float maxRealTime = 0.4f;
        float elapsed = 0f;

        while (elapsed < maxRealTime)
        {
            if (_playerInZone != null && _playerInZone.IsParrying())
            {
                Debug.Log("Parry detected");
                ReflectProjectile(_playerInZone.transform);
                ResetTime();
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ResetTime();

        Collide(null);
    }

    protected void ResetTime()
    {
        if (Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }

    Transform FindClosestEnemy()
    {
        float minDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (Collider2D col in Physics2D.OverlapCircleAll(transform.position, 10f, enemyLayer))
        {
            float dist = Vector2.Distance(transform.position, col.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = col.transform;
            }
        }

        return closest;
    }

    public void ReflectBackwards()
    {
        if (_hasBeenReflected) return;

        Transform target = _shooter != null ? _shooter : FindClosestEnemy();
        if (target == null)
        {
            Debug.Log("No target found to reflect");
            return;
        }

        Vector2 direction = (target.position - transform.position).normalized;
        _rb.linearVelocity = direction * _reflectedSpeed;
        _hasBeenReflected = true;
        transform.right = direction;
    }

    public bool HasBeenReflected => _hasBeenReflected;
    public bool IsParryable => isParryable;
}
