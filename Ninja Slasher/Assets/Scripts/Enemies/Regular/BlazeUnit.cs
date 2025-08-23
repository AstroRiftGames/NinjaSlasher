using UnityEngine;

public class BlazeUnit : RangeEnemy
{
    [SerializeField] Transform _body;
    public override void TryAttack()
    {
        AimCannon(_dirToTarget);
        base.TryAttack();
    }

    private void AimCannon(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _body.rotation = Quaternion.Euler(0, 0,angle);
    }
}
