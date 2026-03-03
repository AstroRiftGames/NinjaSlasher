using UnityEngine;

public class PlatformAudioContext : MonoBehaviour
{
    private PlatformAudioSet _audioSet;

    public PlatformAudioSet Audio => _audioSet;

    public void Initialize(AudioSet audioSet)
    {
        _audioSet = audioSet as PlatformAudioSet;
    }
}
