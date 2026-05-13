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
    [SerializeField] private Button _musicButton;
    [SerializeField] private Button _sfxButton;
    [SerializeField] private RestartConfirmationPopUp _restartConfirmationPopUp;
    [SerializeField] private BackToLevelSelectionConfirmationPopUp _backToLevelSelectionConfirmationPopUp;

    [Header("Background")]
    [SerializeField] private Image _backgroundImage;

    [Header("Info")]
    [SerializeField] private GameObject _infoRoot;
    [SerializeField] private TextMeshProUGUI _totalStarsText;
    [SerializeField] private TextMeshProUGUI _livesAmountText;

    private Texture2D _backgroundTexture;
    private AudioSettingsUI _audioSettingsUI;

    protected override void Awake()
    {
        base.Awake();
        _audioSettingsUI = GetComponentInParent<AudioSettingsUI>();
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
        BindButton(_resumeButton, OnResumeClicked, "resume");
        BindButton(_restartButton, OnRestartClicked, "restart");
        BindButton(_quitButton, OnQuitClicked, "quit");
        BindButton(_musicButton, OnMusicClicked, "music");
        BindButton(_sfxButton, OnSfxClicked, "sfx");
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction callback, string buttonName)
    {
        if (button == null)
        {
            Debug.LogWarning($"[PauseOverlay] {buttonName} button is not assigned.");
            return;
        }

        button.onClick.AddListener(callback);
    }

    protected override void OnShown()
    {
        CaptureScreen();

        PauseController.Instance?.RequestPause(PauseSource.PauseOverlay);
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
    }

    protected override void OnHideAnimationCompleted()
    {
        PauseController.Instance?.ReleasePause(PauseSource.PauseOverlay);
        UIManager.Instance?.SetGameplayHUDTopRightInfoVisible(true);
        UIEvents.RaisePause(false);
    }

    private void OnResumeClicked()
    {
        Hide();
    }

    private void OnMusicClicked()
    {
        if (_audioSettingsUI == null)
        {
            Debug.LogWarning("[PauseOverlay] AudioSettingsUI is not assigned.");
            return;
        }

        _audioSettingsUI.MusicButtonPushed();
    }

    private void OnSfxClicked()
    {
        if (_audioSettingsUI == null)
        {
            Debug.LogWarning("[PauseOverlay] AudioSettingsUI is not assigned.");
            return;
        }

        _audioSettingsUI.SFXButtonPushed();
    }

    private void OnRestartClicked()
    {
        bool canRestart = true;
        if (LevelSessionManager.Instance != null && LifeManager.Instance != null)
        {
            bool includePendingExitCost = LifeManager.Instance.HasPendingDeduction();
            canRestart = LevelSessionManager.Instance.CanStartLevelAttempt(includePendingExitCost);
        }
        else if (LifeManager.Instance != null)
        {
            canRestart = LifeManager.Instance.CanPlay();
        }

        if (!canRestart)
        {
            Debug.Log($"[PauseOverlay] Restart blocked: No available lives. realLives={LifeManager.Instance?.GetRealLives()} | unlimited={LifeManager.Instance?.HasTimedUnlimitedLives}");
            UIEvents.RequestShowNoLivesModal();
            Hide();
            return;
        }

        Debug.Log("[PauseOverlay] Restart authorized by UI validation. Showing confirmation pop-up.");
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
            Destroy(_backgroundTexture);
            _backgroundTexture = null;
        }

        _resumeButton?.onClick.RemoveListener(OnResumeClicked);
        _restartButton?.onClick.RemoveListener(OnRestartClicked);
        _quitButton?.onClick.RemoveListener(OnQuitClicked);
        _musicButton?.onClick.RemoveListener(OnMusicClicked);
        _sfxButton?.onClick.RemoveListener(OnSfxClicked);
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
}
