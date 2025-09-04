
public class SentinelVulnerableState<SentinelStates> : State<SentinelStates>
{
    DemolitionSentinel _sentinel;
    public SentinelVulnerableState(DemolitionSentinel sentinel)
    {
        _sentinel = sentinel;
    }

    public override void Enter()
    {
        _sentinel.Animator.SetBool("isVulnerable", true);
        _sentinel.Core.enabled = true;
    }

    public override void Sleep()
    {
        _sentinel.Animator.SetBool("isVulnerable", false);
        _sentinel.Core.enabled = false;
    }
}