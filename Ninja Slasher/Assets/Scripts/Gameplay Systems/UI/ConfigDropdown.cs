using UnityEngine;
using UnityEngine.UI;

public class ConfigDropdown : MonoBehaviour
{
    [SerializeField] private Animator _configPanelAnim;
    [SerializeField] private bool _isOpen = false;
    [SerializeField] private Button _toggleButton;
    [SerializeField] private Button _musicButton;
    [SerializeField] private Button _sfxButton;
    [SerializeField] private Button _profileButton;
    [SerializeField] private Button _hapticButton;
    private static readonly int OpenTrigger = Animator.StringToHash("Open");
    private static readonly int CloseTrigger = Animator.StringToHash("Close");

    private UIAudioContext _audioContext;
    private AudioSettingsUI _audioSettingsUI;

    private void Awake()
    {
        _audioContext = GetComponentInParent<UIAudioContext>();
        _audioSettingsUI = GetComponent<AudioSettingsUI>();

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
        SetupButtons();
    }

    private void OnEnable()
    {
        UIEvents.OnBlockingPanelShown += OnBlockingPanelShown;
    }

    private void OnDisable()
    {
        UIEvents.OnBlockingPanelShown -= OnBlockingPanelShown;
    }

    private void OnBlockingPanelShown(string source)
    {
        Debug.Log($"[ConfigDropdown] Reset requested by blocking panel show | Source={source}");
        ResetToIdle();
    }

    private void ResetToIdle()
    {
        if (!_isOpen) return;

        _configPanelAnim.Rebind();
        _configPanelAnim.ResetTrigger(OpenTrigger);
        _configPanelAnim.ResetTrigger(CloseTrigger);
        _configPanelAnim.Play("Idle", 0, 0f);
        _configPanelAnim.Update(0f);
        _isOpen = false;
    }

    private void SetupButtons()
    {
        BindButton(_toggleButton, OpenCloseConfigPanel, "toggle");
        BindButton(_musicButton, OnMusicClicked, "music");
        BindButton(_sfxButton, OnSfxClicked, "sfx");
        BindButton(_profileButton, OnProfileClicked, "profile");
        BindButton(_hapticButton, OnHapticClicked, "haptic");
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction callback, string buttonName)
    {
        if (button == null)
        {
            Debug.LogWarning($"[ConfigDropdown] {buttonName} button is not assigned.");
            return;
        }

        button.onClick.AddListener(callback);
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

    private void OnMusicClicked()
    {
        if (_audioSettingsUI == null)
        {
            Debug.LogWarning("[ConfigDropdown] AudioSettingsUI is not assigned.");
            return;
        }

        _audioSettingsUI.MusicButtonPushed();
    }

    private void OnSfxClicked()
    {
        if (_audioSettingsUI == null)
        {
            Debug.LogWarning("[ConfigDropdown] AudioSettingsUI is not assigned.");
            return;
        }

        _audioSettingsUI.SFXButtonPushed();
    }

    private void OnProfileClicked()
    {
        UIEvents.RequestShowProfileModal();
    }

    private void OnHapticClicked()
    {
        if (_audioSettingsUI == null)
        {
            Debug.LogWarning("[ConfigDropdown] AudioSettingsUI is not assigned.");
            return;
        }

        _audioSettingsUI.HapticFeedbackPushed();
    }

    private void OnDestroy()
    {
        _toggleButton?.onClick.RemoveListener(OpenCloseConfigPanel);
        _musicButton?.onClick.RemoveListener(OnMusicClicked);
        _sfxButton?.onClick.RemoveListener(OnSfxClicked);
        _profileButton?.onClick.RemoveListener(OnProfileClicked);
        _hapticButton?.onClick.RemoveListener(OnHapticClicked);
    }
}
