using UnityEngine;

public class RangeEnemy : Enemy
{
    private Transform _target;
    private Vector2 _dirToTarget;

    [Header("LOS Stats")]
    [SerializeField] private float _range;
    [SerializeField] private LayerMask _obstaclesLayer;
    private bool _hasLOS;
    private bool _hasTarget;

    [Header("Attack stats")]
    [SerializeField] private float _cooldDown;
    [SerializeField] private GameObject _projectilePrefab;
    private float _lastAttack;

    protected override void OnEnable()
    {
        _target = FindAnyObjectByType<Controller>().transform;
    }

    protected override void Update()
    {
        base.Update();

        UpdateTarget();

        if (_hasTarget && _hasLOS)
        {
            TryAttack();
        }
    }

    private void UpdateTarget()
    {
        _hasTarget = _target != null;
        if(_hasTarget)
        {
            _dirToTarget = _target.position - transform.position;
            _hasLOS = CheckLOS();
        }
    }

    private void TryAttack()
    {
        if(CheckCooldown())
        {
            Attack();
        }
    }

    private bool CheckCooldown()
    {
        return Time.time >= _lastAttack + _cooldDown;
    }

    private bool CheckLOS()
    {
        float distance = _dirToTarget.magnitude;
        if (distance > _range) return false;

        bool hit = Physics2D.Raycast(transform.position, _dirToTarget.normalized, distance, _obstaclesLayer).collider != null;
#if UNITY_EDITOR
        Debug.DrawRay(transform.position, _dirToTarget.normalized*distance, Color.red, _cooldDown/2);
#endif
        if (hit) return false;

        return true;
    }

    private void Attack()
    {
        _lastAttack = Time.time;
        Instantiate(_projectilePrefab, transform.position, Quaternion.identity).GetComponent<Projectile>().Initialize(_dirToTarget.normalized);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _range);
    }
#endif
}
