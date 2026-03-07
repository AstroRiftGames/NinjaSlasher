using UnityEngine;

public class FlyingEnemy : Enemy
{
    [SerializeField] private float _speed;

    [Header("LOS Stats")]
    [SerializeField] private float _range;
    
    private Vector2 _dirToTarget;
    private Vector2 _target;

    public void Start()
    {
        UpdateTarget(transform.position);
    }
    public override void CustomUpdate()
    {
        if(_player != null && CheckLOS(_player.position))
        {
            UpdateTarget();
        }

        if (CheckDistance())
        {
            AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.move, transform.position);
            _rb.linearVelocity = _dirToTarget.normalized * _speed;
        }
        else
        {
            AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.idle, transform.position);
            _animator.SetTrigger("OnStop");
            _rb.linearVelocity = Vector2.zero;
        }
    }

    private bool CheckDistance()
    {
        float distance = Vector2.Distance(transform.position, _target);
        return distance >= .1f;
    }

    private bool CheckLOS(Vector2 target)
    {
        Vector2 vector = target - (Vector2)transform.position;
        float distance = vector.magnitude;
        if (distance > _range) return false;

        bool hit = Physics2D.Raycast(transform.position, vector.normalized, distance, _obstaclesLayer).collider != null;
        return !hit;
    }

    public virtual void UpdateTarget()
    {
        _animator.SetTrigger("OnDetection");
        _target = _player.position;
        _dirToTarget = _target - (Vector2)transform.localToWorldMatrix.GetPosition();
    }

    private void UpdateTarget(Vector2 target)
    {
        _target = target;
        _dirToTarget = target - (Vector2)transform.localToWorldMatrix.GetPosition();
    }

    public override void Die()
    {
        AudioService.Instance.StopSFX(_audioContext.Audio.move);
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.death, transform.position);
        base.Die();
    }
}
