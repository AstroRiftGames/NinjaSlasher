using UnityEngine;
using UnityEngine.UI;

public class PauseOverlay : UIOverlayBase
{
    [Header("Pause Buttons")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _quitButton;

    private bool _wasPausedBeforeShow = false;

    protected override void Awake()
    {
        base.Awake();
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
    }

    protected override void OnShown()
    {
        _wasPausedBeforeShow = Time.timeScale == 0f;

        if (!_wasPausedBeforeShow)
            Time.timeScale = 0f;

        UIEvents.RaisePause(true);
    }

    protected override void OnHidden()
    {
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

    private void OnDestroy()
    {
        if (_resumeButton != null)
            _resumeButton.onClick.RemoveAllListeners();

        if (_restartButton != null)
            _restartButton.onClick.RemoveAllListeners();

        if (_quitButton != null)
            _quitButton.onClick.RemoveAllListeners();
    }
}
