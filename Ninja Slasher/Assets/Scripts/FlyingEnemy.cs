using Unity.VisualScripting;
using UnityEngine;

public class FlyingEnemy : Enemy
{
    [SerializeField] private float _speed;

    [Header("LOS Stats")]
    [SerializeField] private float _range;
    [SerializeField] private LayerMask _obstaclesLayer;
    
    private Vector2 _dirToTarget;
    private Vector2 _target;

    public override void Start()
    {
        base.Start();
        UpdateTarget(transform.position);
    }
    public virtual void Update()
    {
        if(_player != null && CheckLOS(_player.position)) UpdateTarget();

        if (CheckDistance())
        {
            _rb.linearVelocity = _dirToTarget.normalized * _speed;
        }
        else
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }
    
    private bool CheckDistance()
    {
        float distance = Vector2.Distance(transform.position, _target);
        return distance >= .25f;
    }

    private bool CheckLOS(Vector2 target)
    {
        Vector2 vector = target - (Vector2)transform.position;
        float distance = vector.magnitude;
        if (distance > _range) return false;

        bool hit = Physics2D.Raycast(transform.position, vector.normalized, distance, _obstaclesLayer).collider != null;
        return !hit;
    }

    private void UpdateTarget()
    {
        _target = _player.position;
        _dirToTarget = _target - (Vector2)transform.localToWorldMatrix.GetPosition();
    }

    private void UpdateTarget(Vector2 target)
    {
        _target = target;
        _dirToTarget = target - (Vector2)transform.localToWorldMatrix.GetPosition();
    }
}
