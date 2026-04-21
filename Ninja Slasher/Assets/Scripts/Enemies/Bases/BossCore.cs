using UnityEngine;

public class BossCore : MonoBehaviour
{
    protected BossEnemy _boss;
    protected Collider2D _collider;

    protected virtual void Awake()
    {
        TryGetComponent(out Collider2D collider);
        _collider = collider;
        _boss = GetComponentInParent<BossEnemy>();
    }

    protected virtual void OnEnable()
    {
        if (_collider != null) _collider.enabled = true;
    }

    protected virtual void OnDisable()
    {
        if (_collider != null) _collider.enabled = false;
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (_boss != null && _boss.IsVulnerable)
            {
                OnVulnerableHit(collision);
            }
            else
            {
                if (collision.TryGetComponent(out PlayerController controller))
                {
                    controller.Die();
                }
            }
        }
    }

    protected virtual void OnVulnerableHit(Collider2D collision)
    {
        if (_boss != null)
        {
            _boss.Die();
        }
    }
}
