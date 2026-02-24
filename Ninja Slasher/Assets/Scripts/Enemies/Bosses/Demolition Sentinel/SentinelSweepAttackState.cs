using System.Collections;
using System.Threading.Tasks;
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
        _sentinel.Animator.SetTrigger("onSweep");
        _sentinel.SentinelAudio.PlaySweep();
        _sentinel.SetTargetDirection(Vector2.down);

        yield return new WaitForSeconds(1.5f);

        _sentinel._isSweepAttacking = false;
        _sentinel.SetIsAttacking(false);
        _sentinel.SetJustAttacked(true);
    }
}