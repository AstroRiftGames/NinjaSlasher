using UnityEngine;

public class ConfigDropdown : MonoBehaviour
{
    [SerializeField] private Animator _configPanelAnim;
    [SerializeField] private bool _isOpen = false;

    private UIAudioContext _audioContext;

    private void Awake()
    {
        _audioContext = GetComponentInParent<UIAudioContext>();
    }

    public void OpenCloseConfigPanel()
    {
        //AudioManager.Instance.PlaySFX(SFXClip.UI_ShowConfig);

        AudioService.Instance?.PlaySFX(_audioContext.Audio.showConfig);

        if (_isOpen)
        {
            _configPanelAnim.SetTrigger("Close");
            _isOpen = false;
        }
        else
        {
            _configPanelAnim.SetTrigger("Open");
            _isOpen = true;
        }
    }
}