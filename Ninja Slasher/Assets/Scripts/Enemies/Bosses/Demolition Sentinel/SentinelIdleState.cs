using UnityEngine;

public class SentinelIdleState<SentinelStates> : State<SentinelStates>
{
    DemolitionSentinel _sentinel;
    float _enterTime;
    public SentinelIdleState(DemolitionSentinel sentinel)
    {
        _sentinel = sentinel;
    }

    public override void Enter()
    {
        //_sentinel.SetTargetDirection(Vector2.right);
        _enterTime = Time.time;
    }

    public override void Execute()
    {
        if (Time.time >= _enterTime + _sentinel.Cooldown)
        {   
            _sentinel.SetJustAttacked(false);
        }
    }
}