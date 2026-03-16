using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[Serializable]
//public enum MusicClip
//{
//    Splash,
//    MainMenu,
//    Area1,
//    Area2,
//    Area3,
//    Area4,
//    Area5,
//    BossLevel,
//    Victory,
//    GameOver,
//    Credits
//}

[Serializable]
public enum SFXClip
{
    //E_Hit,
    //E_Shoot,
    //E_Explosion,
    //P_Movement,
    //UI_TapSplashScreen,
    //UI_TransitionSlash,
    //UI_Select,
    UI_PowerUp,
    //UI_Transition,
    //UI_ShowConfig,
    //UI_Claim,
    //E_Scout_Hit,
    //E_Scout_SendReport,
    //E_Blaze_Detection,
    //E_Blaze_Shoot,
    //E_Guard_Detection,
    //E_Guard_Charge,
    //E_Guard_Colision,
    //E_Guard_Death,
    //P_Attack,
    //P_ParrySwing,
    P_ProjectileParried,

    //B_Sentinel_Intro,
    //B_Sentinel_Idle,
    //B_Sentinel_Damaged,
    //B_Sentinel_Defeated,
    //B_Sentinel_Defeated_Idle,
    //B_Sentinel_Vulnerable,
    //B_Sentinel_Vulnerable_Idle,
    //B_Sentinel_Recovered,
    //B_Sentinel_Sweep,

    //P_Die,
    //UI_Victory,
    //UI_Defeat,

    //B_Sentinel_Double_1,
    //B_Sentinel_Double_2,
    //B_Sentinel_Heavy,
    //B_Sentinel_Impact,
    //B_Sentinel_Woosh,

    P_Landing_General,
    //P_Landing_Ground,
    //P_Landing_Stone,
    //P_Landing_Wood,
    //P_KO_1,
    //P_KO_2,
    //P_KO_3,
    //P_KO_4,
    //P_KO_5,
    //P_KO_6,
    //P_KO_7,
    //E_Ricochet_Charge,
    //E_Ricochet_Shoot,
    //E_Ricochet_Death,
    Proj_Ricochet_Bounce,
    Prop_Vase_1,
    Prop_Vase_2,
    Prop_Table,
    Prop_Gong,
    Prop_Chair,
    //E_Nano_Idle,
    //E_Nano_Chase,
    //E_Nano_Hit,
    //E_Nano_Death,
    //E_Mini_Chase,
    //E_Mini_Death,
    B_Drone_Idle,
    B_Drone_FlyAway,
    B_Drone_ConeAttack,
    B_Drone_BurstAttack,
    B_Drone_ReboundAttack,
    B_Drone_ProjectileHit,
    B_Drone_FloorHit,
    B_Drone_PlayerHit,
    //DW_Spin,
    //DW_LeverPull,
    //Reward_PrizeCoins,
    //Reward_Coins,
    //Reward_Prize,
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

//[Serializable]
//public class MusicData
//{
//    public MusicClip clipType;
//    public AudioClipData audioData;
//}

[Serializable]
public class SFXData
{
    public SFXClip clipType;
    public AudioClipData audioData;
}

public class AudioManager : MonoBehaviourSingleton<AudioManager>
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;

    [Header("SFX Clips")]
    [SerializeField] private SFXData[] sfxClips;

    [Header("Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    private Dictionary<SFXClip, AudioClipData> sfxDict = new Dictionary<SFXClip, AudioClipData>();

    private void Start()
    {
        InitializeAudioManager();
    }

    void InitializeAudioManager()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        PopulateDictionaries();
    }

    void PopulateDictionaries()
    {
        foreach (var sfxData in sfxClips)
        {
            if (sfxData.audioData.clip != null)
                sfxDict[sfxData.clipType] = sfxData.audioData;
        }
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