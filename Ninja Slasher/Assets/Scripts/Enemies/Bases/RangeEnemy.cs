using UnityEngine;

public class RangeEnemy : Enemy
{
    private Transform _target;
    protected Vector2 _dirToTarget;

    protected void SetDirToTarget(Vector2 dir) => _dirToTarget = dir;

    [Header("LOS Stats")]
    [SerializeField] private float _LOSRange;
    protected bool _hasLOS;
    private bool _hasTarget;

    [Header("Attack stats")]
    [SerializeField] private float _cooldDown;
    [SerializeField] protected Transform _refPoint;
    [SerializeField] protected Transform _unitCenter;
    private float _lastAttack;
    private ObjectPool<Projectile> _pool;
    protected void SetLastAttack() => _lastAttack = Time.time;
    [SerializeField] [Range(0, 100)] private float _parryableChance;

    protected override void Awake()
    {
        base.Awake();
        _pool = new ObjectPool<Projectile>(_data.Projectile, 5, transform);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        _target = FindAnyObjectByType<PlayerController>().transform;
    }

    public override void CustomUpdate()
    {
        UpdateTarget();

        if (_hasTarget && _hasLOS)
        {
            TryAttack();
        }
    }

    protected void UpdateTarget()
    {
        _hasTarget = _target != null;
        if (_hasTarget)
        {
            _dirToTarget = _target.position - _unitCenter.position;
            _hasLOS = CheckLOS();
        }
    }

    public virtual void TryAttack()
    {
        if (CheckCooldown())
        {
            Attack();
        }
    }

    protected bool CheckCooldown()
    {
        return Time.time >= _lastAttack + _cooldDown;
    }

    private bool CheckLOS()
    {
        Vector2 LOSv = _target.position - transform.position;
        Vector2 LOSvNormalized = LOSv.normalized;
        float distance = LOSv.magnitude;
        if (distance > _LOSRange) return false;

        bool hit = Physics2D.Raycast(transform.position, LOSvNormalized, distance, _obstaclesLayer).collider != null;
        return !hit;
    }

    public virtual void Attack()
    {
        _animator.SetTrigger("OnAttack");
        SetLastAttack();
        Shoot();
    }

    protected void Shoot()
    {
        var projectile = _pool.Get();

        projectile.transform.SetPositionAndRotation(
            _refPoint.position,
            _refPoint.rotation
        );

        projectile.OnRequestDespawn -= HandleProjectileDespawn;
        projectile.OnRequestDespawn += HandleProjectileDespawn;

        projectile.SetParryOverrideProvider(PowerUpManager.Instance);
        projectile.Initialize(_dirToTarget.normalized, transform, DecideParryable());
    }
    protected virtual bool DecideParryable() => Random.Range(0, 100) < _parryableChance;
    private void HandleProjectileDespawn(Projectile projectile)
    {
        if (projectile == null)
            return;

        projectile.OnRequestDespawn -= HandleProjectileDespawn;
        _pool.Release(projectile);
    }

    public override void Die()
    {
        _target = null;
        base.Die();
    }
}
