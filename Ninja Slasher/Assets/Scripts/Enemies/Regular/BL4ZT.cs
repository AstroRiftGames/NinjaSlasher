using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class BL4ZT : RangeEnemy
{
    [SerializeField] float _speed;
    [SerializeField] float _timeToExplode;
    [SerializeField] float _explosionRadius;
    [SerializeField][Range(1, 2)] float _speedMultiplier;
    [SerializeField] Transform[] _nodes;

    private float _currentSpeed;
    private float _activationTime;
    private bool _isActive;
    private Vector2 _destination;

    private float _direction => transform.localScale.x > 0 ? 1 : -1;

    public override void Awake()
    {
        base.Awake();
        _currentSpeed = _speed;
        SetPatrolTarget();
    }
    public override void Update()
    {
        UpdateTarget();
        if(!_isActive)
        {
            if (!_hasLOS)
            {
                if (!CheckTarget(_destination))
                {
                    Move();
                }
                else
                {
                    SetPatrolTarget();
                }
            }
            else
            {
                Activate();
            }
        }
        else
        {
            if(!CheckExplosionTime())
            {
                _destination = GetPlayerPos();
                if(!CheckTarget(_destination))
                {
                    Move();
                }
                else
                {
                    _rb.linearVelocity = Vector2.zero;
                }
            }
            else
            {
                Explode();
            }
        }
    }

    private void Move()
    {
        transform.localScale = new Vector3(_destination.x > transform.localToWorldMatrix.GetPosition().x ? 1 : -1, transform.localScale.y, transform.localScale.z);

        bool thereIsFloor = Physics2D.Raycast(transform.position + transform.right * .5f * _direction + Vector3.down, Vector3.down, .5f, _obstaclesLayer);
        if (thereIsFloor)
        {
            _rb.linearVelocityX = _direction * _currentSpeed;
        }
    }

    private void Explode()
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, _explosionRadius);
        foreach(Collider2D col in cols)
        {
            if (col.CompareTag("Player"))
            {
                col.TryGetComponent(out Controller player);
                player.Die();
            }
        }
        Destroy(gameObject);
    }

    private bool CheckTarget(Vector2 target)
    {
        return Mathf.Abs(target.x - transform.localToWorldMatrix.GetPosition().x) <= 1.25f;
    }

    private bool CheckExplosionTime()
    {
        return Time.time >= _activationTime + _timeToExplode;
    }

    private void Activate()
    {
        _isActive = true;
        _activationTime = Time.time;
        _destination = GetPlayerPos();
        _currentSpeed *= _speedMultiplier;
    }

    private void SetPatrolTarget()
    {
        _rb.linearVelocityX = 0;
        if (_destination == (Vector2)_nodes[0].localToWorldMatrix.GetPosition())
        {
            _destination = _nodes[1].localToWorldMatrix.GetPosition();
        }
        else
        {
            _destination = _nodes[0].localToWorldMatrix.GetPosition();
        }
    }

    private Vector2 GetPlayerPos()
    {
        Vector2 pos = _player.transform.position;
        pos.y = transform.position.y;

        return pos;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 5);
    }
#endif
}
