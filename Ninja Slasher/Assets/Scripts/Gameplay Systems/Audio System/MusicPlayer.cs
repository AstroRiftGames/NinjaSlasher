using System.Collections;
using UnityEngine;

public class MusicPlayer
{
    private readonly AudioSource _source;
    private readonly AudioSettingsSO _settings;
    private readonly MonoBehaviour _coroutineRunner;

    private bool _isMuted = false;

    private float _intendedVolume = 1f;

    public MusicPlayer(AudioSource source, AudioSettingsSO settings, MonoBehaviour coroutineRunner)
    {
        _source = source;
        _settings = settings;
        _coroutineRunner = coroutineRunner;

        _source.loop = true;
        _source.playOnAwake = false;
    }

    public void Play(AudioEvent audioEvent, float fadeTime = 0f)
    {
        if (audioEvent == null || audioEvent.clip == null)
        {
            Debug.LogWarning("MusicPlayer: AudioEvent o clip null.");
            return;
        }

        _intendedVolume = audioEvent.volume * _settings.GetChannelMultiplier(AudioChannel.Music);

        if (IsSameTrackAlreadyPlaying(audioEvent))
        {
            _source.pitch = audioEvent.GetPitch();
            _source.loop = audioEvent.loop;
            _source.volume = _isMuted ? 0f : _intendedVolume;
            return;
        }

        if (fadeTime > 0f && _source.isPlaying)
        {
            _coroutineRunner.StartCoroutine(CrossfadeCoroutine(audioEvent, fadeTime));
        }
        else
        {
            _source.clip   = audioEvent.clip;
            _source.pitch  = audioEvent.GetPitch();
            _source.loop   = audioEvent.loop;
            _source.volume = _isMuted ? 0f : _intendedVolume;
            _source.Play();
        }
    }

    public void Stop(float fadeTime = 0f)
    {
        if (fadeTime > 0f)
        {
            _coroutineRunner.StartCoroutine(FadeOutCoroutine(fadeTime));
        }
        else
        {
            _source.Stop();
        }
    }

    public void Pause() => _source.Pause();
    public void Resume() => _source.UnPause();

    public void UpdateVolume()
    {
        if (_source.isPlaying && !_isMuted)
        {
            _source.volume = _intendedVolume;
        }
    }

    private IEnumerator CrossfadeCoroutine(AudioEvent newEvent, float fadeTime)
    {
        float halfTime    = fadeTime / 2f;
        float startVolume = _source.volume;

        float elapsed = 0f;
        while (elapsed < halfTime)
        {
            elapsed      += Time.deltaTime;
            _source.volume = Mathf.Lerp(startVolume, 0f, elapsed / halfTime);
            yield return null;
        }

        _source.Stop();
        _source.clip  = newEvent.clip;
        _source.pitch = newEvent.GetPitch();
        _source.loop  = newEvent.loop;
        _source.Play();

        _intendedVolume = newEvent.volume * _settings.GetChannelMultiplier(AudioChannel.Music);

        if (!_isMuted)
        {
            elapsed = 0f;
            while (elapsed < halfTime)
            {
                elapsed      += Time.deltaTime;
                _source.volume = Mathf.Lerp(0f, _intendedVolume, elapsed / halfTime);
                yield return null;
            }
            _source.volume = _intendedVolume;
        }
    }

    private bool IsSameTrackAlreadyPlaying(AudioEvent audioEvent)
    {
        return _source.isPlaying &&
               _source.clip == audioEvent.clip &&
               Mathf.Approximately(_source.pitch, audioEvent.GetPitch()) &&
               _source.loop == audioEvent.loop;
    }

    private IEnumerator FadeOutCoroutine(float fadeTime)
    {
        float startVolume = _source.volume;
        float elapsed     = 0f;

        while (elapsed < fadeTime)
        {
            elapsed      += Time.deltaTime;
            _source.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
            yield return null;
        }

        _source.Stop();
        _source.volume = startVolume;
    }

    public void MuteMusic()
    {
        if (_isMuted) return;

        _isMuted = true;
        _source.volume = 0f;
    }

    public void UnmuteMusic()
    {
        if (!_isMuted) return;

        _isMuted = false;
        _source.volume = _intendedVolume;
    }
}
