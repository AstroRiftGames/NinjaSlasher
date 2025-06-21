using Unity.VisualScripting;
using UnityEngine;

public class OM3GA : RangeEnemy
{
    [SerializeField] float _rayDuration;
    [SerializeField] Transform[] _nodes;
    [SerializeField] float _speed;

    private bool _isAttacking;
    private Vector2 _patrolTarget;

    private float _direction => transform.localScale.x > 0 ? 1 : -1;

    public override void Awake()
    {
        base.Awake();
        SetPatrolTarget();
    }
    public override void Update()
    {
        UpdateTarget();
        _isAttacking = _hasLOS;
        if(_isAttacking)
        {
            _rb.linearVelocity = Vector2.zero;
            TryAttack();
        }
        else
        {
            if (!CheckTarget(_patrolTarget))
            {
                Move();
            }
            else if(CheckTarget(_patrolTarget))
            {
                SetPatrolTarget();
            }
        }
    }

    private void Move()
    {
        transform.localScale = new Vector3(_patrolTarget.x > transform.localToWorldMatrix.GetPosition().x ? 1 : -1, transform.localScale.y, transform.localScale.z);

        bool thereIsFloor = Physics2D.Raycast(transform.position + transform.right * .5f * _direction + Vector3.down, Vector3.down, .5f, _obstaclesLayer);
        if (thereIsFloor)
        {
            _rb.linearVelocityX = _direction * _speed;
        }
    }

    private bool CheckTarget(Vector2 target)
    {
        return Vector2.Distance(target, transform.localToWorldMatrix.GetPosition()) <= .5f;
    }

    //public override void Attack()
    //{
    //    RaycastHit2D hit = Physics2D.Raycast(_refPoint.position, _dirToTarget.normalized, 10, _player.gameObject.layer);
    //    Debug.DrawLine(_refPoint.position, (Vector2) _refPoint.position + _dirToTarget.normalized * hit.distance, Color.cyan, _rayDuration);

    //    if(hit && hit.collider.CompareTag("Player"))
    //    {
    //        hit.collider.TryGetComponent(out Controller player);
    //        player.Die();
    //    }
    //    else if (!hit)
    //    {
    //        Debug.Log("Algo anda mal...");
    //    }
    //}

    private void SetPatrolTarget()
    {
        _rb.linearVelocityX = 0;
        if (_patrolTarget == (Vector2)_nodes[0].localToWorldMatrix.GetPosition())
        {
            _patrolTarget = _nodes[1].localToWorldMatrix.GetPosition();
        }
        else
        {
            _patrolTarget = _nodes[0].localToWorldMatrix.GetPosition();
        }
    }
}
