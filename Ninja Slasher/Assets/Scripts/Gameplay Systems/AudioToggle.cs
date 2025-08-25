using UnityEngine;
using UnityEngine.UI;
public class AudioToggle : MonoBehaviour
{
    [Header ("IMAGES")]
    [SerializeField] private Sprite _sfxTurnOnIcon;
    [SerializeField] private Sprite _sfxTurnOffIcon;
    [SerializeField] private Sprite _musicTurnOnIcon;
    [SerializeField] private Sprite _musicTurnOffIcon;

    [SerializeField] private Image _musicImage;
    [SerializeField] private Image _sfxImage;

    //[SerializeField] private AudioSource _audioSource;
    [SerializeField] private bool _musicOn;
    [SerializeField] private bool _sfxOn;

    private void Start()
    {
        _sfxOn = true;
        _musicOn = true;
    }

    public void SFXButtonClicked()
    {
        if (_sfxOn)
        {
            _sfxImage.sprite = _sfxTurnOffIcon;
            _sfxOn = !_sfxOn;
        }
        else
        {
            _sfxImage.sprite = _sfxTurnOnIcon;
            _sfxOn = !_sfxOn;
        }
        AudioManager.Instance.MuteSFX(!_sfxOn);
    }
    
    public void MusicButtonClicked()
    {
        if (_musicOn)
        {
            _musicImage.sprite = _musicTurnOffIcon;
            _musicOn = !_musicOn;
            //_audioSource.mute = false;
        }
        else
        {
            _musicImage.sprite = _musicTurnOnIcon;
            _musicOn = !_musicOn;
            //_audioSource.mute = true;
        }
        AudioManager.Instance.MuteMusic(!_musicOn);
    }
}
