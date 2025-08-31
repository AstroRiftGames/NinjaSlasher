using System.Collections;
using UnityEngine;

public class SentinelDoubleAttackState<SentinelStates> : State<SentinelStates>
{
    DemolitionSentinel _sentinel;
    public SentinelDoubleAttackState(DemolitionSentinel sentinel)
    {
        _sentinel = sentinel;
    }

    public override void Enter()
    {
        _sentinel._isDoubleAttacking = true;
        _sentinel.StartCoroutine(DoubleAttack());
        _sentinel.ChooseAttack();
        _sentinel.SetIsAttacking(true);
    }

    private IEnumerator DoubleAttack()
    {
        _sentinel.Animator.SetTrigger("onDouble");
        for (int i = 0; i < _sentinel.AmountOfAttacks; i++)
        {
            _sentinel.SetTargetDirection();
            _sentinel.AimArm(_sentinel.Balls[_sentinel.IsRightBallTurn ? 0 : 1].PivotPoint);

            yield return new WaitForSeconds(.25f);

            _sentinel.CurrentBall.Throw();
            _sentinel.ChangeBall();

            yield return new WaitForSeconds(1f);
        }
        _sentinel._isDoubleAttacking = false;
        _sentinel.SetIsAttacking(false);
        _sentinel.SetJustAttacked(true);
    }
}