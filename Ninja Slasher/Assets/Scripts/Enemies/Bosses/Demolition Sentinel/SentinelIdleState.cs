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
        AudioManager.Instance.PlayLoopedSFXAtPosition(SFXClip.B_Sentinel_Idle, _sentinel.transform.position);
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
        AudioManager.Instance.StopSFX(SFXClip.B_Sentinel_Idle);
    }
}