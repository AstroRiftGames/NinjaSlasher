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
        yield return new WaitForSeconds(_sentinel.ChargingTime);
        _sentinel.CurrentBall.HeavyThrow(_sentinel.TargetDir);
        _sentinel.ChangeBall();
        _sentinel._isHeavyAttacking = false;
        _sentinel.SetIsAttacking(false);
        _sentinel.SetJustAttacked(true);
        _sentinel.StartCoroutine(_sentinel.ReturnBalls());
    }
}