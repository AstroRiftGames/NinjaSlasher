using UnityEngine;
using UnityEngine.UI;

public class PauseOverlay : UIOverlayBase
{
    [Header("Pause Buttons")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _musicButton;
    [SerializeField] private Button _sfxButton;

    [Header("Dependencies")]
    [SerializeField] private AudioSettingsUI _configToggles;

    private bool _wasPausedBeforeShow = false;

    protected override void Awake()
    {
        base.Awake();

        if (_configToggles == null)
        {
            _configToggles = UIManager.Instance?.GetComponent<AudioSettingsUI>();
        }

        SetupButtons();
    }

    private void SetupButtons()
    {
        if (_resumeButton != null)
            _resumeButton.onClick.AddListener(OnResumeClicked);

        if (_restartButton != null)
            _restartButton.onClick.AddListener(OnRestartClicked);

        if (_quitButton != null)
            _quitButton.onClick.AddListener(OnQuitClicked);

        if (_musicButton != null)
            _musicButton.onClick.AddListener(OnMusicToggled);

        if (_sfxButton != null)
            _sfxButton.onClick.AddListener(OnSFXToggled);
    }

    protected override void OnShown()
    {
        Debug.Log("[PauseOverlay] Juego pausado");

        _wasPausedBeforeShow = Time.timeScale == 0f;

        if (!_wasPausedBeforeShow)
            Time.timeScale = 0f;

        UIEvents.RaisePause(true);
    }

    protected override void OnHidden()
    {
        Debug.Log("[PauseOverlay] Juego reanudado");

        if (!_wasPausedBeforeShow)
            Time.timeScale = 1f;

        UIEvents.RaisePause(false);
    }

    private void OnResumeClicked()
    {
        Hide();
    }

    private void OnRestartClicked()
    {
        Time.timeScale = 1f;
        UIEvents.RequestRestartLevel();
    }

    private void OnQuitClicked()
    {
        Time.timeScale = 1f;
        UIEvents.RaiseQuitToMenuPressed();
    }

    private void OnMusicToggled()
    {
        if (_configToggles != null)
            _configToggles.MusicButtonPushed();
    }

    private void OnSFXToggled()
    {
        if (_configToggles != null)
            _configToggles.SFXButtonPushed();
    }

    private void OnDestroy()
    {
        if (_resumeButton != null)
            _resumeButton.onClick.RemoveAllListeners();

        if (_restartButton != null)
            _restartButton.onClick.RemoveAllListeners();

        if (_quitButton != null)
            _quitButton.onClick.RemoveAllListeners();

        if (_musicButton != null)
            _musicButton.onClick.RemoveAllListeners();

        if (_sfxButton != null)
            _sfxButton.onClick.RemoveAllListeners();
    }
}