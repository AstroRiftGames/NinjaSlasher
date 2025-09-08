using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum MusicClip
{
    Splash,
    MainMenu,
    Area1,
    Area2,
    Area3,
    Area4,
    Area5,
    BossLevel,
    Victory,
    GameOver
}

[Serializable]
public enum SFXClip
{
    E_Hit,
    E_Shoot,
    E_Explosion,
    P_Movement,
    UI_TapSplashScreen,
    UI_TransitionSlash,
    UI_Select,
    UI_PowerUp,
    UI_Transition,
    UI_ShowConfig,
    UI_Claim,
    E_Scout_Hit,
    E_Scout_SendReport,
}

[Serializable]
public class AudioClipData
{
    public AudioClip clip;
    public float volume = 1f;
    public float pitch = 1f;
    public float volumeMultiplier = 1f;
}

[Serializable]
public class MusicData
{
    public MusicClip clipType;
    public AudioClipData audioData;
}

[Serializable]
public class SFXData
{
    public SFXClip clipType;
    public AudioClipData audioData;
}

public class AudioManager : MonoBehaviourSingleton<AudioManager>
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Clips")]
    [SerializeField] private MusicData[] musicClips;

    [Header("SFX Clips")]
    [SerializeField] private SFXData[] sfxClips;

    [Header("Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    private Dictionary<MusicClip, AudioClipData> musicDict = new Dictionary<MusicClip, AudioClipData>();
    private Dictionary<SFXClip, AudioClipData> sfxDict = new Dictionary<SFXClip, AudioClipData>();

    private Coroutine musicFadeCoroutine;
    private MusicClip currentMusicClip;

    private void Start()
    {
        InitializeAudioManager();
        PlayMusic(MusicClip.Splash);
    }

    void InitializeAudioManager()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        PopulateDictionaries();

        UpdateVolumes();
    }

    void PopulateDictionaries()
    {
        foreach (var musicData in musicClips)
        {
            if (musicData.audioData.clip != null)
                musicDict[musicData.clipType] = musicData.audioData;
        }

        foreach (var sfxData in sfxClips)
        {
            if (sfxData.audioData.clip != null)
                sfxDict[sfxData.clipType] = sfxData.audioData;
        }
    }

    public void PlayMusic(MusicClip clipType, bool fadeIn = true)
    {
        if (musicDict.ContainsKey(clipType))
        {
            var audioData = musicDict[clipType];

            if (fadeIn && musicSource.isPlaying)
            {
                StartCoroutine(FadeToNewMusic(audioData, clipType));
            }
            else
            {
                musicSource.clip = audioData.clip;
                musicSource.pitch = audioData.pitch;
                musicSource.Play();
                currentMusicClip = clipType;
            }
        }
        else
        {
            Debug.Log($"Clip de música '{clipType}' no encontrado");
        }
    }

    public void StopMusic(bool fadeOut = true)
    {
        if (fadeOut)
        {
            StartCoroutine(FadeOutMusic());
        }
        else
        {
            musicSource.Stop();
        }
    }

    public void PauseMusic()
    {
        musicSource.Pause();
    }

    public void ResumeMusic()
    {
        musicSource.UnPause();
    }

    public void PlaySFX(SFXClip clipType)
    {
        if (sfxDict.ContainsKey(clipType))
        {
            var audioData = sfxDict[clipType];

            sfxSource.pitch = audioData.pitch;
            sfxSource.PlayOneShot(audioData.clip, audioData.volume * audioData.volumeMultiplier);
        }
        else
        {
            Debug.Log($"Clip de SFX '{clipType}' no encontrado");
        }
    }

    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip != null)
        {
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(clip, volumeMultiplier);
        }
    }

    public void PlaySFXAtPosition(SFXClip clipType, Vector3 position, float volumeMultiplier = 1f)
    {
        if (sfxDict.ContainsKey(clipType))
        {
            var audioData = sfxDict[clipType];
            AudioSource.PlayClipAtPoint(audioData.clip, position,
                audioData.volume * sfxVolume * masterVolume * volumeMultiplier);
        }
    }

    public void PlaySFXWithRandomPitch(SFXClip clipType, float minPitch = 0.8f, float maxPitch = 1.2f, float volumeMultiplier = 1f)
    {
        if (sfxDict.ContainsKey(clipType))
        {
            var audioData = sfxDict[clipType];
            float randomPitch = UnityEngine.Random.Range(minPitch, maxPitch);

            sfxSource.pitch = randomPitch;
            sfxSource.PlayOneShot(audioData.clip, audioData.volume * volumeMultiplier);
        }
    }

    public void MuteMusic(bool state)
    {
        musicSource.mute = state;
    }

    public void MuteSFX(bool state)
    {
        sfxSource.mute = state;
    }
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    void UpdateVolumes()
    {
        musicSource.volume = musicVolume * masterVolume;
        sfxSource.volume = sfxVolume * masterVolume;
    }

    IEnumerator FadeToNewMusic(AudioClipData newAudioData, MusicClip newClipType, float fadeDuration = 1f)
    {
        float startVolume = musicSource.volume;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        musicSource.clip = newAudioData.clip;
        musicSource.pitch = newAudioData.pitch;
        musicSource.Play();
        currentMusicClip = newClipType;

        float targetVolume = musicVolume * masterVolume;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t / fadeDuration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    IEnumerator FadeOutMusic(float fadeDuration = 1f)
    {
        float startVolume = musicSource.volume;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = musicVolume * masterVolume;
    }

    public bool IsMusicPlaying()
    {
        return musicSource.isPlaying;
    }

    public MusicClip GetCurrentMusicClip()
    {
        return currentMusicClip;
    }

    public bool IsPlaying(MusicClip clipType)
    {
        return musicSource.isPlaying && currentMusicClip == clipType;
    }

    public void SetMusicPitch(float pitch)
    {
        musicSource.pitch = pitch;
    }

    public void ResetMusicPitch()
    {
        if (musicDict.ContainsKey(currentMusicClip))
        {
            musicSource.pitch = musicDict[currentMusicClip].pitch;
        }
    }
}