
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
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.B_Sentinel_Vulnerable, _sentinel.transform.position);

        //TODO: Agregar SFX vulnerable idle en loop luego de SFX vulnerable.
    }

    public override void Sleep()
    {
        _sentinel.Animator.SetBool("isVulnerable", false);
        _sentinel.Core.enabled = false;

        //TODO: Detener SFX vulnerable idle antes de reproducir SFX recovered
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.B_Sentinel_Recovered, _sentinel.transform.position);

    }
}