using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PauseOverlay : UIOverlayBase
{
    [Header("Pause Buttons")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private RestartConfirmationPopUp _restartConfirmationPopUp;
    [SerializeField] private BackToLevelSelectionConfirmationPopUp _backToLevelSelectionConfirmationPopUp;

    [Header("Info")]
    [SerializeField] private GameObject _infoRoot;
    [SerializeField] private TextMeshProUGUI _totalStarsText;
    [SerializeField] private TextMeshProUGUI _livesAmountText;

    private bool _wasPausedBeforeShow = false;

    protected override void Awake()
    {
        base.Awake();
        ResolvePopupReferences();
        SetupButtons();
    }

    private void Update()
    {
        if (!IsVisible || _infoRoot == null)
            return;

        RefreshInfo();
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

        RefreshInfo();
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

    private void RefreshInfo()
    {
        if (_totalStarsText != null)
        {
            var (_, _, totalStars) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);
            _totalStarsText.text = totalStars.ToString();
        }

        LifeManager lifeManager = LifeManager.Instance;
        if (lifeManager == null || !lifeManager.IsInitialized)
            return;

        if (_livesAmountText != null)
            _livesAmountText.text = lifeManager.GetDisplayLives().ToString();
    }

    private void OnDailyWheelClicked()
    {
        UIEvents.RequestShowDailyWheelModal();
    }

    private static string FormatUnlimitedLivesTime(TimeSpan remaining)
    {
        if (remaining.TotalHours >= 1d)
            return $"{Mathf.FloorToInt((float)remaining.TotalHours):D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";

        return $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
    }

    private static string FormatNextLifeTimer(TimeSpan remaining)
    {
        return remaining.TotalSeconds > 0d
            ? $"{remaining.Minutes:D2}:{remaining.Seconds:D2}"
            : string.Empty;
    }
}
