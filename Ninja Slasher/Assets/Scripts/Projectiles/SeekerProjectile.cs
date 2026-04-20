using UnityEngine;

public class SeekerProjectile : Projectile
{
    [SerializeField] private float _seekingTime;
    private float _startTime;
    private Transform _target;
    private Vector2 _targetDir;

    public override void SetDirection(Vector2 direction)
    {
        _target = FindAnyObjectByType<Controller>().transform;
        _startTime = Time.time;
    }

    public override void Update()
    {
        if (TryHandleGameplayClosed())
            return;

        if(Time.time >= _startTime + _seekingTime)
        {
            Destroy(gameObject);
        }

        _targetDir = (_target.position - transform.position).normalized;
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(_targetDir * _speed);
    }
}
