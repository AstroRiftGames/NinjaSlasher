using System.Collections;
using UnityEngine;

public class SentinelVulnerableState<SentinelStates> : State<SentinelStates>
{
    DemolitionSentinel _sentinel;
    public SentinelVulnerableState(DemolitionSentinel sentinel)
    {
        _sentinel = sentinel;
    }

    public override void Enter()
    {
        _sentinel.SetVulnerability(true);
        _sentinel.Core.enabled = true;
    }
}