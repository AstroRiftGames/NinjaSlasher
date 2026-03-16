using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio/User Config")]
public class UserConfig : ScriptableObject
{
    private bool _musicEnabled;
    private bool _sfxEnabled;

    public bool MusicEnabled => _musicEnabled;

    public bool SFXEnabled => _sfxEnabled;

    public event Action<bool> OnMusicEnabledChanged;

    public event Action<bool> OnSFXEnabledChanged;

    public event Action OnSaveStateRequested;

    public void InitializeState(bool musicEnabled, bool sfxEnabled)
    {
        _musicEnabled = musicEnabled;
        _sfxEnabled   = sfxEnabled;

        OnMusicEnabledChanged?.Invoke(_musicEnabled);
        OnSFXEnabledChanged?.Invoke(_sfxEnabled);
    }

    public void ToggleMusic()
    {
        _musicEnabled = !_musicEnabled;
        OnMusicEnabledChanged?.Invoke(_musicEnabled);
        OnSaveStateRequested?.Invoke();
    }

    public void ToggleSFX()
    {
        _sfxEnabled = !_sfxEnabled;
        OnSFXEnabledChanged?.Invoke(_sfxEnabled);
        OnSaveStateRequested?.Invoke();
    }
}
