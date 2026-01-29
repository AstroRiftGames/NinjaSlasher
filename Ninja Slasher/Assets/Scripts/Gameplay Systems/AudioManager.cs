using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
    GameOver,
    Credits
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
    E_Blaze_Detection,
    E_Blaze_Shoot,
    E_Guard_Detection,
    E_Guard_Charge,
    E_Guard_Colision,
    E_Guard_Death,
    P_Attack,
    P_ParrySwing,
    P_ProjectileParried,
    B_Sentinel_Intro,
    B_Sentinel_Idle,
    B_Sentinel_Damaged,
    B_Sentinel_Defeated,
    B_Sentinel_Defeated_Idle,
    B_Sentinel_Vulnerable,
    B_Sentinel_Vulnerable_Idle,
    B_Sentinel_Recovered,
    B_Sentinel_Sweep,
    P_Die,
    UI_Victory,
    UI_Defeat,
    B_Sentinel_Double_1,
    B_Sentinel_Double_2,
    B_Sentinel_Heavy,
    B_Sentinel_Impact,
    B_Sentinel_Woosh,
    P_Landing_General,
    P_Landing_Ground,
    P_Landing_Stone,
    P_Landing_Wood,
    P_KO_1,
    P_KO_2,
    P_KO_3,
    P_KO_4,
    P_KO_5,
    P_KO_6,
    P_KO_7,
    E_Ricochet_Charge,
    E_Ricochet_Shoot,
    E_Ricochet_Death,
    Proj_Ricochet_Bounce,
    Prop_Vase_1,
    Prop_Vase_2,
    Prop_Table,
    Prop_Gong,
    Prop_Chair,
    E_Nano_Idle,
    E_Nano_Chase,
    E_Nano_Hit,
    E_Nano_Death,
    E_Mini_Chase,
    E_Mini_Death,
    B_Drone_Idle,
    B_Drone_FlyAway,
    B_Drone_ConeAttack,
    B_Drone_BurstAttack,
    B_Drone_ReboundAttack,
    B_Drone_ProjectileHit,
    B_Drone_FloorHit,
    B_Drone_PlayerHit,
    DW_Spin,
    DW_LeverPull,
    Reward_PrizeCoins,
    Reward_Coins,
    Reward_Prize,
    P_Landing_Slippery,
    P_Landing_Elastic,
    P_Landing_Breakable,
    Plat_Breakable,

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
            Debug.Log($"Clip de m�sica '{clipType}' no encontrado");
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

    public Dictionary<SFXClip, AudioSource> srcDict = new Dictionary<SFXClip, AudioSource>();

    public void PlayLoopedSFXAtPosition(SFXClip clip, Vector3 position, float volumeMultiplier = 1f)
    {
        if (sfxDict.ContainsKey(clip))
        {
            var audioData = sfxDict[clip];

            GameObject newObj = Instantiate(new GameObject(audioData.clip.name), position, Quaternion.identity);
            newObj.transform.SetPositionAndRotation(position, Quaternion.identity);

            AudioSource src = newObj.AddComponent<AudioSource>();
            srcDict.Add(clip, src);
            string msg = "";
            foreach (AudioSource element in srcDict.Values)
            {
                msg += $"{element.gameObject.name}, ";
            }
            Debug.Log(msg);
            src.loop = true;
            src.clip = audioData.clip;
            src.volume = audioData.volume * sfxVolume * masterVolume * volumeMultiplier;

            src.Play();
        }
        else
        {
            Debug.Log($"Clip de SFX '{clip}' no encontrado");
        }
    }

    public void StopSFX(SFXClip clip)
    {
        srcDict.TryGetValue(clip, out AudioSource src);
        if (src == null)
        {
            Debug.Log( clip + " not found in array");
        }
        else
        {
            srcDict.Remove(clip);
            src.Stop();
            Destroy(src.gameObject);
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

    public float GetSFXDuration(SFXClip clipType)
    {
        if (sfxDict.ContainsKey(clipType))
        {
            var audioData = sfxDict[clipType];
            if (audioData.clip != null)
            {
                return audioData.clip.length;
            }
        }

        return 0f;
    }
}