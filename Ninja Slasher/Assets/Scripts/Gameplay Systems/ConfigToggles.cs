using UnityEngine;
using UnityEngine.UI;
public class ConfigToggles : MonoBehaviour
{
    [Header ("IMAGES")]
    [SerializeField] private Sprite _sfxTurnOnIcon;
    [SerializeField] private Sprite _sfxTurnOffIcon;
    [SerializeField] private Sprite _musicTurnOnIcon;
    [SerializeField] private Sprite _musicTurnOffIcon;
    [SerializeField] private Sprite _hapticTurnOnIcon;
    [SerializeField] private Sprite _hapticTurnOffIcon;

    [SerializeField] private Image _musicImage;
    [SerializeField] private Image _sfxImage;
    [SerializeField] private Image _hapticImage;
    [SerializeField] private Image _musicPauseImage;
    [SerializeField] private Image _sfxPauseImage;
    //[SerializeField] private Image _hapticPauseImage;

    [SerializeField] private bool _isMusicOn;
    [SerializeField] private bool _isSfxOn;
    [SerializeField] private bool _isHapticOn;

    private void Start()
    {
        _isSfxOn = true;
        _isMusicOn = true;
        _isHapticOn = UIManager.Instance.IsHapticFeedbackActive;
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
        if (_isSfxOn)
        {
            _sfxImage.sprite = _sfxTurnOffIcon;
            _sfxPauseImage.sprite = _sfxTurnOffIcon;
            _isSfxOn = !_isSfxOn;
        }
        else
        {
            _sfxImage.sprite = _sfxTurnOnIcon;
            _sfxPauseImage.sprite = _sfxTurnOnIcon;
            _isSfxOn = !_isSfxOn;
        }
        AudioManager.Instance.MuteSFX(!_isSfxOn);
    }
    
    public void MusicButtonPushed()
    {
        if (_isMusicOn)
        {
            _musicImage.sprite = _musicTurnOffIcon;
            _musicPauseImage.sprite = _musicTurnOffIcon;
            _isMusicOn = !_isMusicOn;
        }
        else
        {
            _musicImage.sprite = _musicTurnOnIcon;
            _musicPauseImage.sprite = _musicTurnOnIcon;
            _isMusicOn = !_isMusicOn;
        }
        AudioManager.Instance.MuteMusic(!_isMusicOn);
    }
}
