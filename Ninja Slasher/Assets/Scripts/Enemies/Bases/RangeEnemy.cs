using UnityEngine;

public class RangeEnemy : Enemy
{
    private Transform _target;
    protected Vector2 _dirToTarget;

    protected void SetDirToTarget(Vector2 dir) => _dirToTarget = dir;

    [Header("LOS Stats")]
    [SerializeField] private float _range;
    [SerializeField] protected LayerMask _obstaclesLayer;
    protected bool _hasLOS;
    private bool _hasTarget;

    [Header("Attack stats")]
    [SerializeField] private float _cooldDown;
    [SerializeField] protected Transform _refPoint;
    private float _lastAttack;
    protected void SetLastAttack() => _lastAttack = Time.time;

    public override void OnEnable()
    {
        _target = FindAnyObjectByType<Controller>().transform;
    }

    public virtual void Update()
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
        float distance = _dirToTarget.magnitude;
        if (distance > _range) return false;

        bool hit = Physics2D.Raycast(_refPoint.position, _dirToTarget.normalized, distance, _obstaclesLayer).collider != null;
#if UNITY_EDITOR
        Debug.DrawRay(_refPoint.position, _dirToTarget.normalized*distance, Color.red, _cooldDown/2);
#endif
        return !hit;
    }

    public virtual void Attack()
    {
        SetLastAttack();
        Shoot();
    }

    protected void Shoot()
    {
        var projectile = Instantiate(_data.Projectile, _refPoint.position, Quaternion.identity)
                    .GetComponent<Projectile>();
        projectile.Initialize(_dirToTarget.normalized, transform);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _range);
    }
#endif
}
