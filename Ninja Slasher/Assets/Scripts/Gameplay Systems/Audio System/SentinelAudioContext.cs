using System.Collections;
using UnityEngine;

public class SentinelAudioContext : MonoBehaviour
{
    private SentinelAudioSet _audioSet;
    public SentinelAudioSet Audio => _audioSet;

    public void Initialize(AudioSet audioSet)
    {
        _audioSet = audioSet as SentinelAudioSet;
    }

    public void PlayIntro() =>
        AudioService.Instance.PlaySFXAtPosition(_audioSet.intro, transform.position);

    public void StartIdle() =>
        AudioService.Instance.PlaySFX(_audioSet.idleLoop);

    public void StopIdle() =>
        AudioService.Instance.StopSFX(_audioSet.idleLoop);

    public void PlayDoubleAttack(bool right) =>
        AudioService.Instance.PlaySFXAtPosition(
            right ? _audioSet.doubleAttackRight : _audioSet.doubleAttackLeft,
            transform.position
        );

    public void PlaySweep() =>
        AudioService.Instance.PlaySFXAtPosition(_audioSet.sweepAttack, transform.position);

    public void PlayWoosh() =>
        AudioService.Instance.PlaySFXAtPosition(_audioSet.woosh, transform.position);

    public void PlayHeavy() =>
        AudioService.Instance.PlaySFXAtPosition(_audioSet.heavyAttack, transform.position);

    public void PlayImpact() =>
        AudioService.Instance.PlaySFXAtPosition(_audioSet.impact, transform.position);

    public void PlayChainDamaged() =>
        AudioService.Instance.PlaySFXAtPosition(_audioSet.chainDamaged, transform.position);

    public void EnterVulnerable()
    {
        AudioService.Instance.PlaySFXAtPosition(_audioSet.vulnerableEnter, transform.position);
        AudioService.Instance.PlaySFX(_audioSet.vulnerableLoop);
    }

    public void ExitVulnerable()
    {
        AudioService.Instance.StopSFX(_audioSet.vulnerableLoop);
        AudioService.Instance.PlaySFXAtPosition(_audioSet.recovered, transform.position);
    }

    public void Defeated()
    {
        AudioService.Instance.PlaySFXAtPosition(_audioSet.defeated, transform.position);
        AudioService.Instance.PlaySFX(_audioSet.defeatedLoop);
    }

    public IEnumerator VulnerableFeedbackSequence(float delay)
    {
        AudioService.Instance.PlaySFXAtPosition(_audioSet.vulnerableEnter, transform.position);
        yield return new WaitForSeconds(delay);
        AudioService.Instance.PlaySFX(_audioSet.vulnerableLoop);
    }

    public IEnumerator DefeatedFeedbackSequence(float delay)
    {
        AudioService.Instance.PlaySFXAtPosition(_audioSet.defeated, transform.position);
        yield return new WaitForSeconds(delay);
        AudioService.Instance.PlaySFX(_audioSet.defeatedLoop);
    }
}