using System.Collections;
using UnityEngine;

public class ArachnomadreAudioContext : MonoBehaviour
{
    private ArachnomadreAudioSet _audioSet;
    public ArachnomadreAudioSet Audio => _audioSet;

    public void Initialize(AudioSet audioSet)
    {
        _audioSet = audioSet as ArachnomadreAudioSet;
    }

    public void PlayIntro() =>
        AudioService.Instance.PlaySFXAtPosition(_audioSet.intro, transform.position);

    public void StartIdle() =>
        AudioService.Instance.PlaySFX(_audioSet.idleLoop);

    public void StopIdle() =>
        AudioService.Instance.StopSFX(_audioSet.idleLoop);
    public void StartMovement() =>
        AudioService.Instance.PlaySFX(_audioSet.movementLoop);
    public void StopMovement() =>
        AudioService.Instance.StopSFX(_audioSet.movementLoop);

    public void PlayEggSpawn() => AudioService.Instance.PlaySFXAtPosition(_audioSet.eggSpawn, transform.position);
    public void PlaySubmerge() => AudioService.Instance.PlaySFXAtPosition(_audioSet.submerge, transform.position);
    public void PlayEmerge() => AudioService.Instance.PlaySFXAtPosition(_audioSet.emerge, transform.position);

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