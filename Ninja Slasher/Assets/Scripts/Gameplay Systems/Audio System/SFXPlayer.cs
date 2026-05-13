using System.Collections.Generic;
using UnityEngine;

public class SFXPlayer
{
    private readonly ObjectPool<PooledAudioSource> _pool;
    private readonly AudioSettingsSO _settings;
    private readonly Transform _parent;

    private readonly Dictionary<AudioEvent, PooledAudioSource> _loopingSources
    = new Dictionary<AudioEvent, PooledAudioSource>();
    private readonly HashSet<PooledAudioSource> _activeSources = new();

    private bool isMuted = false;

    public SFXPlayer(PooledAudioSource prefab, int poolSize, AudioSettingsSO settings, Transform parent)
    {
        _settings = settings;
        _parent = parent;
        _pool = new ObjectPool<PooledAudioSource>(prefab, poolSize, parent);
    }

    public void Play(AudioEvent audioEvent)
    {
        if (isMuted) return;
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
        pooled.Play(audioEvent, _settings, Vector3.zero, _pool);
        RegisterActiveSource(audioEvent, pooled);

        if (audioEvent.loop)
        {
            _loopingSources[audioEvent] = pooled;
        }
    }

    public void PlayAtPosition(AudioEvent audioEvent, Vector3 position)
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
        pooled.transform.position = position;
        pooled.Play(audioEvent, _settings, position, _pool);
        RegisterActiveSource(audioEvent, pooled);

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
            ReleaseSource(pooled);
            _loopingSources.Remove(audioEvent);
        }

        if (_activeSources.Count == 0)
            return;

        List<PooledAudioSource> activeSnapshot = new List<PooledAudioSource>(_activeSources);
        for (int i = 0; i < activeSnapshot.Count; i++)
        {
            PooledAudioSource activeSource = activeSnapshot[i];
            if (activeSource == null || activeSource.CurrentAudioEvent != audioEvent)
                continue;

            ReleaseSource(activeSource);
        }
    }

    public void StopAll()
    {
        if (_activeSources.Count == 0)
        {
            _loopingSources.Clear();
            return;
        }

        List<PooledAudioSource> activeSnapshot = new List<PooledAudioSource>(_activeSources);
        for (int i = 0; i < activeSnapshot.Count; i++)
        {
            ReleaseSource(activeSnapshot[i]);
        }

        _loopingSources.Clear();
    }

    public void MuteSFX()
    {
        if (isMuted) return;

        isMuted = true;

        foreach (var kvp in _loopingSources)
        {
            if (kvp.Value != null && kvp.Value.Source != null)
                kvp.Value.Source.volume = 0f;
        }
    }

    public void UnmuteSFX()
    {
        if (!isMuted) return;

        isMuted = false;

        foreach (var kvp in _loopingSources)
        {
            var audioEvent = kvp.Key;
            var pooled     = kvp.Value;

            if (pooled != null && pooled.Source != null)
                pooled.Source.volume =
                    audioEvent.volume *
                    _settings.GetChannelMultiplier(audioEvent.channel);
        }
    }

    public void RefreshVolumes()
    {
        foreach (var kvp in _loopingSources)
        {
            var audioEvent = kvp.Key;
            var pooled = kvp.Value;

            if (pooled != null && pooled.Source != null)
            {
                pooled.Source.volume =
                    audioEvent.volume *
                    _settings.GetChannelMultiplier(audioEvent.channel);
            }
        }
    }

    private void RegisterActiveSource(AudioEvent audioEvent, PooledAudioSource pooled)
    {
        if (pooled == null)
            return;

        pooled.ReleasedToPool -= OnSourceReleasedToPool;
        pooled.ReleasedToPool += OnSourceReleasedToPool;
        _activeSources.Add(pooled);
    }

    private void ReleaseSource(PooledAudioSource pooled)
    {
        if (pooled == null)
            return;

        pooled.Stop();
        _pool.Release(pooled);
    }

    private void OnSourceReleasedToPool(PooledAudioSource pooled)
    {
        if (pooled == null)
            return;

        _activeSources.Remove(pooled);

        AudioEvent loopEventToRemove = null;
        foreach (var kvp in _loopingSources)
        {
            if (kvp.Value != pooled)
                continue;

            loopEventToRemove = kvp.Key;
            break;
        }

        if (loopEventToRemove != null)
            _loopingSources.Remove(loopEventToRemove);
    }
}
