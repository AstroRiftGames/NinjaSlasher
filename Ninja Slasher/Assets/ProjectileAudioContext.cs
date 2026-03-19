using UnityEngine;

public class ProjectileAudioContext : MonoBehaviour
{
    private ProjectileAudioSet _audioSet;

    public ProjectileAudioSet   Audio => _audioSet;

    public void Initialize(AudioSet audioSet)
    {
        _audioSet = audioSet as ProjectileAudioSet;
    }
}
