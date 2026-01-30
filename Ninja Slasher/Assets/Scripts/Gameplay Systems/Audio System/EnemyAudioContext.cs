using UnityEngine;

public class EnemyAudioContext : MonoBehaviour
{
    private EnemyAudioSet _audioSet;

    public EnemyAudioSet Audio => _audioSet;

    public void Initialize(EnemyAudioSet audioSet)
    {
        _audioSet = audioSet;
    }
}
