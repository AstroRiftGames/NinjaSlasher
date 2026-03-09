using System.Collections;
using UnityEngine;

public class MultiAttackDroneAudioContext : MonoBehaviour
{
    private MultiAttackDroneAudioSet _audioSet;
    public MultiAttackDroneAudioSet Audio => _audioSet;

    public void Initialize(AudioSet audioSet)
    {
        _audioSet = audioSet as MultiAttackDroneAudioSet;
    }

    public void PlayIntro() =>
        AudioService.Instance.PlaySFXAtPosition(_audioSet.intro, transform.position);

    public void StartIdle() =>
        AudioService.Instance.PlaySFX(_audioSet.idleLoop);

    public void StopIdle() =>
        AudioService.Instance.StopSFX(_audioSet.idleLoop);
    public void StartFlyAway() =>
        AudioService.Instance.PlaySFX(_audioSet.flyAwayLoop);
    public void StopFlyAway() =>
        AudioService.Instance.StopSFX(_audioSet.flyAwayLoop);

    public void PlayReboundAttack() => AudioService.Instance.PlaySFXAtPosition(_audioSet.rebound, transform.position);
    public void PlayConeAttack() => AudioService.Instance.PlaySFXAtPosition(_audioSet.cone, transform.position);
    public void PlayBurstAttack() => AudioService.Instance.PlaySFXAtPosition(_audioSet.burst, transform.position);
    public void PlayPlayerHit() => AudioService.Instance.PlaySFXAtPosition(_audioSet.hitByPlayer, transform.position);
    public void PlayProjHit() => AudioService.Instance.PlaySFXAtPosition(_audioSet.hitByProj, transform.position);
    public void PlayGroundHit() => AudioService.Instance.PlaySFXAtPosition(_audioSet.hitByGround, transform.position);

    public void Defeated()
    {
        AudioService.Instance.PlaySFXAtPosition(_audioSet.defeated, transform.position);
    }
}