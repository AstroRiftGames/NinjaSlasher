using System.Collections;
using UnityEditor;
using UnityEngine;

public class ScoutBot : Enemy
{
    [SerializeField] float _deathTime;
    [SerializeField] float _detectionRange;
    public override void Die()
    {
        _animator.SetTrigger("OnHit");
        Destroy(gameObject, _deathTime);
    }

    public void Update()
    {
        _animator.SetBool("IsAlert", CheckLOS(_player));
    }


    private bool CheckLOS(Transform target)
    {
        Vector2 dirToTarget = (target.position - transform.position).normalized;
        float disToTarget = Vector2.Distance(transform.position, target.position);

        Debug.DrawRay(transform.position, dirToTarget * _detectionRange, Color.red, 1f);

        bool _noObstacles = !Physics2D.Raycast(transform.position, dirToTarget, _detectionRange, _obstaclesLayer);
        bool _player = Physics2D.Raycast(transform.position, dirToTarget, _detectionRange, _playerLayer);

        return _player && _noObstacles;
    }
}
