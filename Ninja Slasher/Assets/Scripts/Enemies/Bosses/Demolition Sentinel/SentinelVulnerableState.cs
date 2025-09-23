
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
        _sentinel.Animator.SetBool("isVulnerable", true);
        _sentinel.Core.enabled = true;
        _sentinel.StartCoroutine(PlayFeedback(3f));
    }

    public override void Sleep()
    {
        _sentinel.Animator.SetBool("isVulnerable", false);
        _sentinel.Core.enabled = false;

        AudioManager.Instance.StopSFX(SFXClip.B_Sentinel_Vulnerable_Idle);
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.B_Sentinel_Recovered, _sentinel.transform.position);

    }
    IEnumerator PlayFeedback(float time)
    {
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.B_Sentinel_Vulnerable, _sentinel.transform.position);
        yield return new WaitForSeconds(time);
        AudioManager.Instance.PlayLoopedSFXAtPosition(SFXClip.B_Sentinel_Vulnerable_Idle, _sentinel.transform.position);
    }
}