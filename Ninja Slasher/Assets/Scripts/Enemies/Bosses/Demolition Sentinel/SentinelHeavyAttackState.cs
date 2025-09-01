using System.Collections;
using UnityEngine;

public class SentinelHeavyAttackState<SentinelStates> : State<SentinelStates>
{
    DemolitionSentinel _sentinel;
    public SentinelHeavyAttackState(DemolitionSentinel sentinel)
    {
        _sentinel = sentinel;
    }

    public override void Enter()
    {
        _sentinel._isHeavyAttacking = true;
        _sentinel.SetIsAttacking(true);
        _sentinel.ChooseAttack();
        _sentinel.StartCoroutine(HeavyAttack());
    }

    private IEnumerator HeavyAttack()
    {
        _sentinel.Animator.SetTrigger("onHeavy");

        _sentinel.SetTargetDirection();
        _sentinel.AimArm(_sentinel.Balls[_sentinel.IsRightBallTurn ? 0 : 1].PivotPoint);

        yield return new WaitForSeconds(.25f);

        _sentinel.CurrentBall.HeavyThrow();

        while (_sentinel.CurrentBall.IsOut)
        {
            yield return null;
        }

        _sentinel.SetTargetDirection(Vector2.down);
        _sentinel.AimArm(_sentinel.CurrentBall.PivotPoint);
        if (_sentinel.Balls.Length >= 2) _sentinel.ChangeBall();

        _sentinel._isHeavyAttacking = false;
        _sentinel.SetIsAttacking(false);
        _sentinel.SetJustAttacked(true);
    }
}