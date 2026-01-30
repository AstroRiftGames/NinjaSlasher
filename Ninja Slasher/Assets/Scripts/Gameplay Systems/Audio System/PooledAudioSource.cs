using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PooledAudioSource : MonoBehaviour, IPoolable
{
    public AudioSource Source { get; private set; }
    private ObjectPool<PooledAudioSource> _pool;
    private Coroutine _releaseCoroutine;

    private void Awake()
    {
        Source = GetComponent<AudioSource>();
        Source.playOnAwake = false;
    }

    #region IPoolable

    public void OnSpawn()
    {
        if (Source != null)
        {
            Source.Stop();
            Source.clip = null;
            Source.loop = false;
            Source.pitch = 1f;
            Source.spatialBlend = 0f;
        }

        if (_releaseCoroutine != null)
        {
            StopCoroutine(_releaseCoroutine);
            _releaseCoroutine = null;
        }
    }

    public void OnDespawn()
    {
        if (_releaseCoroutine != null)
        {
            StopCoroutine(_releaseCoroutine);
            _releaseCoroutine = null;
        }

        if (Source != null)
        {
            Source.Stop();
            Source.clip = null;
        }
    }

    #endregion

    public void Play(AudioEvent audioEvent, AudioSettings settings, Vector3 position, ObjectPool<PooledAudioSource> pool)
    {
        _pool = pool;

        Source.clip = audioEvent.clip;
        Source.volume = audioEvent.volume * settings.GetChannelMultiplier(audioEvent.channel);
        Source.pitch = audioEvent.GetPitch();
        Source.loop = audioEvent.loop;

        if (audioEvent.spatial)
        {
            Source.spatialBlend = audioEvent.spatialBlend;
            Source.maxDistance = audioEvent.maxDistance;
            Source.rolloffMode = AudioRolloffMode.Linear;
            transform.position = position;
        }
        else
        {
            Source.spatialBlend = 0f;
        }

        Source.Play();

        if (!audioEvent.loop)
        {
            float duration = audioEvent.clip.length / Source.pitch;
            _releaseCoroutine = StartCoroutine(AutoReleaseCoroutine(duration));
        }
    }

    private IEnumerator AutoReleaseCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_pool != null)
        {
            _pool.Release(this);
        }

        _releaseCoroutine = null;
    }

    public void Stop()
    {
        if (_releaseCoroutine != null)
        {
            StopCoroutine(_releaseCoroutine);
            _releaseCoroutine = null;
        }

        if (Source != null)
        {
            Source.Stop();
            Source.clip = null;
        }
    }
}