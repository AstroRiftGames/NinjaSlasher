using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Referencia al ScriptableObject de configuración")]
    [SerializeField] private UserConfig audioConfig;

    [Header("Imagen del botón de música")]
    [SerializeField] private Image musicButtonImage;

    [Header("Sprites de música")]
    [SerializeField] private Sprite musicTurnOnIcon;
    [SerializeField] private Sprite musicTurnOffIcon;

    [Header("Imagen del botón de SFX")]
    [SerializeField] private Image sfxButtonImage;

    [Header("Sprites de SFX")]
    [SerializeField] private Sprite sfxTurnOnIcon;
    [SerializeField] private Sprite sfxTurnOffIcon;

    [Header("Haptic")]
    [SerializeField] private Image _hapticImage;
    [SerializeField] private Sprite _hapticTurnOnIcon;
    [SerializeField] private Sprite _hapticTurnOffIcon;

    private bool _isHapticOn;

    private void OnEnable()
    {
        if (audioConfig == null) return;

        audioConfig.OnMusicEnabledChanged += UpdateMusicVisual;
        audioConfig.OnSFXEnabledChanged   += UpdateSFXVisual;

        RefreshUI();
    }

    private void OnDisable()
    {
        if (audioConfig == null) return;

        audioConfig.OnMusicEnabledChanged -= UpdateMusicVisual;
        audioConfig.OnSFXEnabledChanged   -= UpdateSFXVisual;
    }

    public void MusicButtonPushed() => audioConfig?.ToggleMusic();

    public void SFXButtonPushed() => audioConfig?.ToggleSFX();

    public void HapticFeedbackPushed()
    {
        _isHapticOn = !_isHapticOn;

        if (_hapticImage != null)
            _hapticImage.sprite = _isHapticOn ? _hapticTurnOnIcon : _hapticTurnOffIcon;
    }

    public void RefreshUI()
    {
        if (audioConfig == null) return;

        UpdateMusicVisual(audioConfig.MusicEnabled);
        UpdateSFXVisual(audioConfig.SFXEnabled);
    }

    private void UpdateMusicVisual(bool isEnabled)
    {
        if (musicButtonImage != null)
            musicButtonImage.sprite = isEnabled ? musicTurnOnIcon : musicTurnOffIcon;
    }

    private void UpdateSFXVisual(bool isEnabled)
    {
        if (sfxButtonImage != null)
            sfxButtonImage.sprite = isEnabled ? sfxTurnOnIcon : sfxTurnOffIcon;
    }
}
