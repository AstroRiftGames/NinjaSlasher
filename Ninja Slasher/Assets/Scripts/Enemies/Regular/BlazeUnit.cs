using UnityEngine;

public class BlazeUnit : RangeEnemy
{
    [SerializeField] Transform _body;
    bool _isAlert;
    bool _hasPlayedDetectionSFX;
    float _lastDetectionTime;
    [SerializeField] float _resetDelay = 5f;

    public override void Update()
    {
        base.Update();
        bool canDetectPlayer = _isAlert || _hasLOS;

        if (canDetectPlayer && !_hasPlayedDetectionSFX)
        {
            AudioManager.Instance.PlaySFXAtPosition(SFXClip.E_Blaze_Detection, transform.position);
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
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.E_Blaze_Shoot, transform.position);
        base.Attack();
    }
}
