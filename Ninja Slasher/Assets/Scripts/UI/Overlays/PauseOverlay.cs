using UnityEngine;
using UnityEngine.UI;

public class PauseOverlay : UIOverlayBase
{
    [Header("Pause Buttons")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private RestartConfirmationPopUp _restartConfirmationPopUp;
    [SerializeField] private BackToLevelSelectionConfirmationPopUp _backToLevelSelectionConfirmationPopUp;

    private bool _wasPausedBeforeShow = false;

    protected override void Awake()
    {
        base.Awake();
        ResolvePopupReferences();
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
        _restartConfirmationPopUp?.HideImmediate();
        _backToLevelSelectionConfirmationPopUp?.HideImmediate();

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
        if (_restartConfirmationPopUp != null)
        {
            _restartConfirmationPopUp.ShowConfirmation(ConfirmRestartLevel);
            return;
        }

        Debug.LogWarning("[PauseOverlay] RestartConfirmationPopUp not found. Falling back to direct restart.");
        ConfirmRestartLevel();
    }

    private void ConfirmRestartLevel()
    {
        Hide();
        Time.timeScale = 1f;
        UIEvents.RequestRestartLevel();
    }

    private void OnQuitClicked()
    {
        if (_backToLevelSelectionConfirmationPopUp != null)
        {
            _backToLevelSelectionConfirmationPopUp.ShowConfirmation(ConfirmQuitToLevelSelection);
            return;
        }

        Debug.LogWarning("[PauseOverlay] BackToLevelSelectionConfirmationPopUp not found. Falling back to direct quit.");
        ConfirmQuitToLevelSelection();
    }

    private void ResolvePopupReferences()
    {
        if (_restartConfirmationPopUp != null)
            return;

        UIManager uiManager = GetComponentInParent<UIManager>(true);
        if (uiManager != null)
            _restartConfirmationPopUp = uiManager.GetComponentInChildren<RestartConfirmationPopUp>(true);

        if (_restartConfirmationPopUp == null)
            _restartConfirmationPopUp = GetComponentInChildren<RestartConfirmationPopUp>(true);

        if (_backToLevelSelectionConfirmationPopUp != null)
            return;

        if (uiManager != null)
            _backToLevelSelectionConfirmationPopUp = uiManager.GetComponentInChildren<BackToLevelSelectionConfirmationPopUp>(true);

        if (_backToLevelSelectionConfirmationPopUp == null)
            _backToLevelSelectionConfirmationPopUp = GetComponentInChildren<BackToLevelSelectionConfirmationPopUp>(true);
    }

    private void ConfirmQuitToLevelSelection()
    {
        Hide();
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
