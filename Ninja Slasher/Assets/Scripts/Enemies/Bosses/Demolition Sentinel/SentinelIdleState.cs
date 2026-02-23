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
        _enterTime = Time.time;
        _sentinel.SentinelAudio.StartIdle();
    }

    public override void Execute()
    {
        if (Time.time >= _enterTime + _sentinel.Cooldown)
        {   
            _sentinel.SetJustAttacked(false);
        }
    }

    public override void Sleep()
    {
        base.Sleep();
        _sentinel.SentinelAudio.StopIdle();
    }
}