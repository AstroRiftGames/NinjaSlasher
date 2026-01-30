using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio/Audio Config")]
public class AudioConfig : ScriptableObject
{
    private const string MUSIC_ENABLED_KEY = "MusicEnabled";
    private const string SFX_ENABLED_KEY = "SFXEnabled";

    [Header("Audio State")]
    [SerializeField] private bool musicEnabled = true;
    [SerializeField] private bool sfxEnabled = true;

    public bool MusicEnabled => musicEnabled;
    public bool SFXEnabled => sfxEnabled;

    public event Action<bool> OnMusicEnabledChanged;
    public event Action<bool> OnSFXEnabledChanged;

    public void LoadFromPlayerPrefs()
    {
        musicEnabled = PlayerPrefs.GetInt(MUSIC_ENABLED_KEY, 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt(SFX_ENABLED_KEY, 1) == 1;
    }

    private void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetInt(MUSIC_ENABLED_KEY, musicEnabled ? 1 : 0);
        PlayerPrefs.SetInt(SFX_ENABLED_KEY, sfxEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleMusic()
    {
        musicEnabled = !musicEnabled;
        SaveToPlayerPrefs();
        OnMusicEnabledChanged?.Invoke(musicEnabled);
    }

    public void ToggleSFX()
    {
        sfxEnabled = !sfxEnabled;
        SaveToPlayerPrefs();
        OnSFXEnabledChanged?.Invoke(sfxEnabled);
    }
}