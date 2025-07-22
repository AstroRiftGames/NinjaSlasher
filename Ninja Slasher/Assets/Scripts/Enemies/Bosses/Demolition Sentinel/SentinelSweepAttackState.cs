using System.Collections;
using TMPro;
using UnityEngine;

public class SentinelSweepAttackState<SentinelStates> : State<SentinelStates>
{
    DemolitionSentinel _sentinel;
    public SentinelSweepAttackState(DemolitionSentinel sentinel)
    {
        _sentinel = sentinel;
    }

    public override void Enter()
    {
        _sentinel._isSweepAttacking = true;
        _sentinel.SetIsAttacking(true);
        _sentinel.ChooseAttack();
        _sentinel.StartCoroutine(SweepAttack());
    }

    private IEnumerator SweepAttack()
    {
        _sentinel.Animator.SetFloat("SpeedMultiplier", 1 / _sentinel.SweepDuration);
        _sentinel.Animator.SetTrigger(_sentinel.IsRightBallTurn ? "SweepAttack-Right" : "SweepAttack-Left");
        yield return new WaitForSeconds(_sentinel.SweepDuration);
        _sentinel._isSweepAttacking = false;
        _sentinel.SetIsAttacking(false);
        _sentinel.SetJustAttacked(true);
        _sentinel.ChangeBall();
    }
}