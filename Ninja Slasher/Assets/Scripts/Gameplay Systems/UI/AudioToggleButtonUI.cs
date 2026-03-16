using UnityEngine;
using UnityEngine.UI;

public class AudioToggleButtonUI : MonoBehaviour
{
    // Determina si este botón controla música o SFX.
    public enum AudioToggleType { Music, SFX }

    [Header("Configuración")]
    [SerializeField] private UserConfig _audioConfig;
    [SerializeField] private AudioToggleType _toggleType = AudioToggleType.Music;

    [Header("Visual")]
    [SerializeField] private Image _buttonImage;
    [SerializeField] private Sprite _enabledSprite;
    [SerializeField] private Sprite _disabledSprite;

    private void OnEnable()
    {
        if (_audioConfig == null) return;

        if (_toggleType == AudioToggleType.Music)
            _audioConfig.OnMusicEnabledChanged += UpdateVisual;
        else
            _audioConfig.OnSFXEnabledChanged += UpdateVisual;

        RefreshVisual();
    }

    private void OnDisable()
    {
        if (_audioConfig == null) return;

        if (_toggleType == AudioToggleType.Music)
            _audioConfig.OnMusicEnabledChanged -= UpdateVisual;
        else
            _audioConfig.OnSFXEnabledChanged -= UpdateVisual;
    }

    public void OnButtonClicked()
    {
        if (_audioConfig == null) return;

        if (_toggleType == AudioToggleType.Music)
            _audioConfig.ToggleMusic();
        else
            _audioConfig.ToggleSFX();
    }

    private void RefreshVisual()
    {
        bool currentState = _toggleType == AudioToggleType.Music
            ? _audioConfig.MusicEnabled
            : _audioConfig.SFXEnabled;

        UpdateVisual(currentState);
    }

    private void UpdateVisual(bool isEnabled)
    {
        if (_buttonImage == null) return;
        _buttonImage.sprite = isEnabled ? _enabledSprite : _disabledSprite;
    }
}
