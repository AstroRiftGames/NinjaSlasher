using System.Collections;
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
        Quaternion initRot = _sentinel.CurrentBall.Anchor.rotation;
        Quaternion targetRot = initRot * Quaternion.Euler(0, 0, 180 * (_sentinel.IsRightBallTurn ? -1 : 1));

        float n = 0;
        while (n < 1)
        {
            n += Time.deltaTime / (_sentinel.SweepDuration/2);
            _sentinel.CurrentBall.Anchor.rotation = Quaternion.Slerp(initRot, targetRot, n);
            yield return null;
        }

        n = 0;
        while (n < 1)
        {
            n += Time.deltaTime / (_sentinel.SweepDuration/2);
            _sentinel.CurrentBall.Anchor.rotation = Quaternion.Slerp(targetRot, initRot, n);
            yield return null;
        }

        _sentinel._isSweepAttacking = false;
        _sentinel.SetIsAttacking(false);
        _sentinel.SetJustAttacked(true);
        _sentinel.ChangeBall();
        _sentinel.StartCoroutine(_sentinel.ReturnBalls());
    }
}