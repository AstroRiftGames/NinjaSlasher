using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioConfig audioConfig;

    [Header("Music")]
    [SerializeField] private Image musicImage;
    [SerializeField] private Image musicPauseImage;
    [SerializeField] private Sprite musicTurnOnIcon;
    [SerializeField] private Sprite musicTurnOffIcon;

    [Header("SFX")]
    [SerializeField] private Image sfxImage;
    [SerializeField] private Image sfxPauseImage;
    [SerializeField] private Sprite sfxTurnOnIcon;
    [SerializeField] private Sprite sfxTurnOffIcon;

    [Header ("IMAGES")]
    [SerializeField] private Sprite _hapticTurnOnIcon;
    [SerializeField] private Sprite _hapticTurnOffIcon;

    [SerializeField] private Image _hapticImage;
    [SerializeField] private bool _isHapticOn;

    private void Start()
    {
        audioConfig.OnMusicEnabledChanged += UpdateMusicUI;
        audioConfig.OnSFXEnabledChanged += UpdateSFXUI;

        UpdateMusicUI(audioConfig.MusicEnabled);
        UpdateSFXUI(audioConfig.SFXEnabled);

        _isHapticOn = UIManager.Instance.IsHapticFeedbackActive;
    }

    private void OnDestroy()
    {
        if (audioConfig != null)
        {
            audioConfig.OnMusicEnabledChanged -= UpdateMusicUI;
            audioConfig.OnSFXEnabledChanged -= UpdateSFXUI;
        }
    }

    public void HapticFeedbackPushed()
    {
        if (_isHapticOn)
        {
            _hapticImage.sprite = _hapticTurnOffIcon;
            //_hapticPauseImage.sprite = _hapticTurnOffIcon;
            _isHapticOn = !_isHapticOn;
        }
        else
        {
            _hapticImage.sprite = _hapticTurnOnIcon;
            //_hapticPauseImage.sprite = _hapticTurnOnIcon;
            _isHapticOn = !_isHapticOn;
        }
    }
    public void SFXButtonPushed()
    {
        audioConfig.ToggleSFX();
    }

    public void MusicButtonPushed()
    {
        audioConfig.ToggleMusic();
    }

    private void UpdateMusicUI(bool isEnabled)
    {
        Sprite targetSprite = isEnabled ? musicTurnOnIcon : musicTurnOffIcon;

        if (musicImage != null)
            musicImage.sprite = targetSprite;

        if (musicPauseImage != null)
            musicPauseImage.sprite = targetSprite;
    }

    private void UpdateSFXUI(bool isEnabled)
    {
        Sprite targetSprite = isEnabled ? sfxTurnOnIcon : sfxTurnOffIcon;

        if (sfxImage != null)
            sfxImage.sprite = targetSprite;

        if (sfxPauseImage != null)
            sfxPauseImage.sprite = targetSprite;
    }
}
