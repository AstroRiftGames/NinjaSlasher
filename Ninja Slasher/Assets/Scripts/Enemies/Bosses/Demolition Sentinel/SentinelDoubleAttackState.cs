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
        for (int i = 0; i < _sentinel.AmountOfAttacks; i++)
        {
            _sentinel.SetTargetDirection();
            _sentinel.CurrentBall.Throw(_sentinel.TargetDir);
            _sentinel.ChangeBall();
            yield return new WaitForSeconds(_sentinel.TimeBetweenAttacks);
        }
        _sentinel._isDoubleAttacking = false;
        _sentinel.SetIsAttacking(false);
        _sentinel.SetJustAttacked(true);
    }
}