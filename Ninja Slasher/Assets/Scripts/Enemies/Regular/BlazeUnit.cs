using UnityEngine;

public class BlazeUnit : RangeEnemy
{
    [SerializeField] Transform _body;
    bool _isAlert;
    bool _hasPlayedDetectionSFX;
    float _lastDetectionTime;
    [SerializeField] float _resetDelay = 5f;

    public override void CustomUpdate()
    {
        if (GameManager.Instance.PlayerHasDied) return;
        base.CustomUpdate();
        bool canDetectPlayer = _isAlert || _hasLOS;

        if (canDetectPlayer && !_hasPlayedDetectionSFX)
        {
            AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.detection, transform.position);
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
    }
    public override void TryAttack()
    {
        AimCannon(_dirToTarget);
        base.TryAttack();
    }

    private void AimCannon(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _body.rotation = Quaternion.Euler(0, 0, angle);
    }

    public override void Attack()
    {
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.shoot, transform.position);
        base.Attack();
    }
}
