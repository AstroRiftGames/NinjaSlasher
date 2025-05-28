using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float _speed;
    [SerializeField] private float _reflectedSpeed;
    [SerializeField] private Transform _shooter;
    [SerializeField] private LayerMask enemyLayer;

    private Rigidbody2D _rb;
    private bool _hasBeenReflected = false;

    private bool _timeSlowed = false;
    private Controller _playerInZone;

    private void OnEnable()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _rb.linearVelocity = transform.right * _speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("ParryZone") && !_timeSlowed)
        {
            _playerInZone = collision.GetComponentInParent<Controller>();
            if (_playerInZone != null)
            {
                StartCoroutine(SlowTimeUntilParry());
            }
        }

        if (!_hasBeenReflected && collision.CompareTag("Player"))
        {
            if (_playerInZone != null && _playerInZone.IsParrying())
            {
                ReflectProjectile(_playerInZone.transform);
            }
            else
            {
                DamagePlayer();
            }

            ResetTime();
            Destroy(gameObject);
        }

        if (_hasBeenReflected && collision.CompareTag("Enemy"))
        {
            DamageEnemy(collision.gameObject);
            Destroy(gameObject);
        }
    }


    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!_hasBeenReflected && collision.CompareTag("ParryZone") && _timeSlowed)
        {
            _playerInZone = collision.GetComponentInParent<Controller>();
            if (_playerInZone != null && _playerInZone.IsParrying())
            {
                Debug.Log("Parry detected");
                ReflectProjectile(_playerInZone.transform);
                ResetTime();
            }
        }
    }

    void ReflectProjectile(Transform playerTransform)
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

    void DamagePlayer()
    {
        Debug.Log("player damaged");
    }

    void DamageEnemy(GameObject enemy)
    {
        Destroy(enemy);
    }

    public void SetShooter(Transform shooter)
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
    }

    private void ResetTime()
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
}
