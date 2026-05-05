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

    [Header("Background")]
    [SerializeField] private UnityEngine.UI.Image _backgroundImage;

    [Header("Info")]
    [SerializeField] private GameObject _infoRoot;
    [SerializeField] private TextMeshProUGUI _totalStarsText;
    [SerializeField] private TextMeshProUGUI _livesAmountText;

    private Texture2D _backgroundTexture;
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
        CaptureScreen();
        
        PauseController.Instance.RequestPause(PauseSource.PauseOverlay);
        RefreshInfo();
        UIManager.Instance?.SetGameplayHUDTopRightInfoVisible(false);
        UIEvents.RaisePause(true);
    }

    private void CaptureScreen()
    {
        if (_backgroundImage == null)
            return;

        int width = Screen.width;
        int height = Screen.height;

        if (_backgroundTexture == null || _backgroundTexture.width != width)
        {
            _backgroundTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        }

        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.targetTexture = renderTexture;
            mainCamera.Render();
            mainCamera.targetTexture = null;
        }

        _backgroundTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        _backgroundTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);

        Sprite sprite = Sprite.Create(_backgroundTexture, new Rect(0, 0, width, height), Vector2.one * 0.5f);
        _backgroundImage.sprite = sprite;
    }

    protected override void OnHidden()
    {
        _restartConfirmationPopUp?.HideImmediate();
        _backToLevelSelectionConfirmationPopUp?.HideImmediate();

        PauseController.Instance.ReleasePause(PauseSource.PauseOverlay);
        UIManager.Instance?.SetGameplayHUDTopRightInfoVisible(true);
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
        UIEvents.RaiseQuitToMenuPressed();
    }

    private void OnDestroy()
    {
        if (_backgroundTexture != null)
        {
            UnityEngine.Object.Destroy(_backgroundTexture);
            _backgroundTexture = null;
        }

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
