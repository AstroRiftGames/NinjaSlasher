using System.Collections.Generic;
using UnityEngine;

public class SFXPlayer
{
    private readonly ObjectPool<PooledAudioSource> _pool;
    private readonly AudioSettings _settings;
    private readonly Transform _parent;

    private readonly Dictionary<AudioEvent, PooledAudioSource> _loopingSources
    = new Dictionary<AudioEvent, PooledAudioSource>();

    private bool isMuted = false;

    public SFXPlayer(PooledAudioSource prefab, int poolSize, AudioSettings settings, Transform parent)
    {
        _settings = settings;
        _parent = parent;
        _pool = new ObjectPool<PooledAudioSource>(prefab, poolSize, parent);
    }

    public void Play(AudioEvent audioEvent)
    {
        if (audioEvent == null || audioEvent.clip == null || isMuted)
        {
            Debug.LogWarning("SFXPlayer: AudioEvent o clip null.");
            return;
        }

        if (audioEvent.loop && _loopingSources.ContainsKey(audioEvent))
        {
            return;
        }

        PooledAudioSource pooled = _pool.Get();
        pooled.Play(audioEvent, _settings, Vector3.zero, _pool);

        if (audioEvent.loop)
        {
            _loopingSources[audioEvent] = pooled;
        }
    }

    public void PlayAtPosition(AudioEvent audioEvent, Vector3 position)
    {
        if (audioEvent == null || audioEvent.clip == null)
        {
            Debug.LogWarning("SFXPlayer: AudioEvent o clip null.");
            return;
        }

        if (audioEvent.loop && _loopingSources.ContainsKey(audioEvent))
        {
            return;
        }

        PooledAudioSource pooled = _pool.Get();
        pooled.transform.position = position;
        pooled.Play(audioEvent, _settings, position, _pool);

        if (audioEvent.loop)
        {
            _loopingSources[audioEvent] = pooled;
        }
    }

    public void Stop(AudioEvent audioEvent)
    {
        if (audioEvent == null) return;

        if (_loopingSources.TryGetValue(audioEvent, out var pooled))
        {
            pooled.Stop();
            _pool.Release(pooled);
            _loopingSources.Remove(audioEvent);
        }
    }

    public void StopAll()
    {
        foreach (var kvp in _loopingSources)
        {
            kvp.Value.Stop();
            _pool.Release(kvp.Value);
        }

        _loopingSources.Clear();
    }

    public void MuteSFX()
    {
        isMuted = true;
    }

    public void UnmuteSFX()
    {
        isMuted = false;
    }
}