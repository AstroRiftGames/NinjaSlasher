using UnityEngine;

public class ConfigDropdown : MonoBehaviour
{
    [SerializeField] private Animator _configPanelAnim;
    [SerializeField] private bool _isOpen = false;
    private static readonly int OpenTrigger = Animator.StringToHash("Open");
    private static readonly int CloseTrigger = Animator.StringToHash("Close");

    private UIAudioContext _audioContext;

    private void Awake()
    {
        _audioContext = GetComponentInParent<UIAudioContext>();

        if (_configPanelAnim == null)
        {
            enabled = false;
            return;
        }

        if (_isOpen)
        {
            _configPanelAnim.Play("Opened", 0, 1f);
        }
        else
        {
            _configPanelAnim.Play("Idle", 0, 0f);
        }

        _configPanelAnim.Update(0f);
    }

    public void OpenCloseConfigPanel()
    {
        AudioService.Instance?.PlaySFX(_audioContext?.Audio.showConfig);

        _configPanelAnim.ResetTrigger(OpenTrigger);
        _configPanelAnim.ResetTrigger(CloseTrigger);

        if (_isOpen)
        {
            _configPanelAnim.SetTrigger(CloseTrigger);
            _isOpen = false;
        }
        else
        {
            _configPanelAnim.SetTrigger(OpenTrigger);
            _isOpen = true;
        }
    }
}