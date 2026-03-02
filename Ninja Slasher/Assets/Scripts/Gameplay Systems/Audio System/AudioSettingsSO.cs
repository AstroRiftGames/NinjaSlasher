using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio/Audio Settings")]
public class AudioSettingsSO : ScriptableObject
{
    private const string MASTER_KEY = "Audio_Master";
    private const string MUSIC_KEY = "Audio_Music";
    private const string SFX_KEY = "Audio_SFX";
    private const string UI_KEY = "Audio_UI";

    [Header("Global Volume")]
    [Range(0f, 1f)] public float master = 1f;

    [Header("Channels")]
    [Range(0f, 1f)] public float music = 0.7f;
    [Range(0f, 1f)] public float sfx = 0.8f;
    [Range(0f, 1f)] public float ui = 1f;

    public float GetChannelMultiplier(AudioChannel channel)
    {
        return channel switch
        {
            AudioChannel.Music => music * master,
            AudioChannel.UI => ui * master,
            AudioChannel.SFX => sfx * master,
            _ => master
        };
    }

    public void Load()
    {
        master = PlayerPrefs.GetFloat(MASTER_KEY, master);
        music = PlayerPrefs.GetFloat(MUSIC_KEY, music);
        sfx = PlayerPrefs.GetFloat(SFX_KEY, sfx);
        ui = PlayerPrefs.GetFloat(UI_KEY, ui);
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(MASTER_KEY, master);
        PlayerPrefs.SetFloat(MUSIC_KEY, music);
        PlayerPrefs.SetFloat(SFX_KEY, sfx);
        PlayerPrefs.SetFloat(UI_KEY, ui);
        PlayerPrefs.Save();
    }

    public void ResetToDefaults()
    {
        master = 1f;
        music = 0.7f;
        sfx = 0.8f;
        ui = 1f;
        Save();
    }
}