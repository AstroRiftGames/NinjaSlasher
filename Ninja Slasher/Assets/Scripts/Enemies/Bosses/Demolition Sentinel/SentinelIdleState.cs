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
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.B_Sentinel_Idle, _sentinel.transform.position);

        //TODO: Configurar SFX idle en loop.
    }

    public override void Execute()
    {
        if (Time.time >= _enterTime + _sentinel.Cooldown)
        {   
            _sentinel.SetJustAttacked(false);
        }
    }

    //TODO: Detener SFX idle en método Sleep()
}