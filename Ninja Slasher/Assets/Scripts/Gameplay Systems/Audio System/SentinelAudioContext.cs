using UnityEngine;

public class SentinelAudioContext : MonoBehaviour
{
    [SerializeField] private SentinelAudioSet audio;

    public void PlayIntro() =>
        AudioService.Instance.PlaySFXAtPosition(audio.intro, transform.position);

    public void StartIdle() =>
        AudioService.Instance.PlaySFX(audio.idleLoop);

    public void StopIdle() =>
        AudioService.Instance.StopSFX(audio.idleLoop);

    public void PlayDoubleAttack(bool right) =>
        AudioService.Instance.PlaySFXAtPosition(
            right ? audio.doubleAttackRight : audio.doubleAttackLeft,
            transform.position
        );

    public void PlaySweep() =>
        AudioService.Instance.PlaySFXAtPosition(audio.sweepAttack, transform.position);

    public void EnterVulnerable()
    {
        AudioService.Instance.PlaySFXAtPosition(audio.vulnerableEnter, transform.position);
        AudioService.Instance.PlaySFX(audio.vulnerableLoop);
    }

    public void ExitVulnerable()
    {
        AudioService.Instance.StopSFX(audio.vulnerableLoop);
        AudioService.Instance.PlaySFXAtPosition(audio.recovered, transform.position);
    }

    public void Defeated()
    {
        AudioService.Instance.PlaySFXAtPosition(audio.defeated, transform.position);
        AudioService.Instance.PlaySFX(audio.defeatedLoop);
    }
}
