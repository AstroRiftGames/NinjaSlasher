using UnityEngine;

public class SFXPlayer
{
    private readonly ObjectPool<PooledAudioSource> _pool;
    private readonly AudioSettings _settings;
    private readonly Transform _parent;

    public SFXPlayer(PooledAudioSource prefab, int poolSize, AudioSettings settings, Transform parent)
    {
        _settings = settings;
        _parent = parent;
        _pool = new ObjectPool<PooledAudioSource>(prefab, poolSize, parent);
    }

    public void Play(AudioEvent audioEvent)
    {
        if (audioEvent == null || audioEvent.clip == null)
        {
            Debug.LogWarning("SFXPlayer: AudioEvent o clip null.");
            return;
        }

        PooledAudioSource pooled = _pool.Get();
        pooled.Play(audioEvent, _settings, Vector3.zero, _pool);
    }

    public void PlayAtPosition(AudioEvent audioEvent, Vector3 position)
    {
        if (audioEvent == null || audioEvent.clip == null)
        {
            Debug.LogWarning("SFXPlayer: AudioEvent o clip null.");
            return;
        }

        PooledAudioSource pooled = _pool.Get();
        pooled.transform.position = position;
        pooled.Play(audioEvent, _settings, position, _pool);
    }

    public void StopAll()
    {
        Debug.LogWarning("SFXPlayer.StopAll() requiere tracking de sources activos.");
    }
}