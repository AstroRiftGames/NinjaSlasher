using Managers;
using UnityEngine;

public class RangeEnemy : Enemy
{
    private Transform _target;
    protected Vector2 _dirToTarget;

    protected void SetDirToTarget(Vector2 dir) => _dirToTarget = dir;

    [Header("LOS Stats")]
    [SerializeField] private float _range;
    protected bool _hasLOS;
    private bool _hasTarget;

    [Header("Attack stats")]
    [SerializeField] private float _cooldDown;
    [SerializeField] protected Transform _refPoint;
    private float _lastAttack;
    private GenericPool<Projectile> _pool;
    protected void SetLastAttack() => _lastAttack = Time.time;

    public override void Awake()
    {
        base.Awake();
        _pool = new GenericPool<Projectile>(_data.Projectile, 5, transform);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        _target = FindAnyObjectByType<Controller>().transform;
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
        if(_hasTarget)
        {
            _dirToTarget = _target.position - _refPoint.position;
            _hasLOS = CheckLOS();
        }
    }

    public virtual void TryAttack()
    {
        if(CheckCooldown())
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
        if (distance > _range) return false;

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
        projectile.transform.SetPositionAndRotation(_refPoint.position, Quaternion.identity);
        projectile.Initialize(_dirToTarget.normalized, transform, _pool);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _range);
    }
#endif
}
