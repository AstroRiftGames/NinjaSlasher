using UnityEngine;

public class SentinelIdleState<SentinelStates> : State<SentinelStates>
{
    DemolitionSentinel _sentinel;
    public SentinelIdleState(DemolitionSentinel sentinel)
    {
        _sentinel = sentinel;
    }

    public override void Enter()
    {
        
    }

    public override void Execute()
    {
        
    }

    public override void Sleep()
    {
        
    }
}