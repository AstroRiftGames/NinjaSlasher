using Managers;
using UnityEngine;

public class ScoutBot : Enemy
{
    [SerializeField] float _detectionRange;
    bool _isAlert;
    bool _hasPlayedDetectionSFX;
    float _lastDetectionTime;
    [SerializeField] float _resetDelay = 5f;

    public override void CustomUpdate()
    {
        bool hasLOS = CheckLOS(_player);
        bool canDetectPlayer = _isAlert || hasLOS;

        if (canDetectPlayer && !_hasPlayedDetectionSFX)
        {
            AudioManager.Instance.PlaySFXAtPosition(SFXClip.E_Scout_SendReport, transform.position);
            _hasPlayedDetectionSFX = true;
        }

        if (canDetectPlayer)
        {
            _lastDetectionTime = Time.time;
        }

        if (!canDetectPlayer && Time.time - _lastDetectionTime > _resetDelay)
        {
            _hasPlayedDetectionSFX = false;
        }

        _animator.SetBool("IsAlert", canDetectPlayer);
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

    public override void Die()
    {
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.E_Scout_Hit, transform.position);
        base.Die();
    }
}
