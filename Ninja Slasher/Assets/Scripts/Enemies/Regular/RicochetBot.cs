using Unity.VisualScripting;
using UnityEngine;

public class RicochetBot : RangeEnemy
{
    public override void CustomUpdate()
    {
        TryAttack();
    }
    private void SetRandomDirection()
    {
        Vector2 newVector = (Vector2) transform.up + new Vector2(Random.Range(-1, 1), Random.Range(-1, 1));
        SetDirToTarget(newVector.normalized);
    }

    public override void Attack()
    {
        SetRandomDirection();
        base.Attack();
    }
}
