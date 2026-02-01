using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class UIManager : MonoBehaviourSingleton<UIManager>
{
    [Header("MANAGERS")]
    private ButtonManager _buttonManager;
    private GameplayUIManager _gameplayUIManager;
    private PreGameUIManager _preGameUIManager;

    [Header("OVERLAYS")]
    [SerializeField] private PauseOverlay _pauseOverlay;
    [SerializeField] private NoLivesOverlay _noLivesOverlay;
    [SerializeField] private LifeLostOverlay _lifeLostOverlay;

    [Header("SCREENS")]
    [SerializeField] private SplashScreen _splashScreen;
    [SerializeField] private PreGameScreen _preGameScreen;
    [SerializeField] private LevelsScreen _levelsScreen;

    [Header("MODALS")]
    [SerializeField] private CreditsModal _creditsModal;
    [SerializeField] private ProfileModal _profileModal;
    [SerializeField] private DailyRewardModal _dailyRewardModal;
    [SerializeField] private DailyWheelModal _dailyWheelModal;
    [SerializeField] private StoreModal _storeModal;
    [SerializeField] private ResultsModal _resultsModal;

    [Header("HUD")]
    [SerializeField] private GameplayHUD _gameplayHUD;

    [Header("DAILY SYSTEMS")]
    [SerializeField] private DailyWheelUI _dailyWheelUI;

    public bool IsHapticFeedbackActive => _isHapticFeedbackActive;
    private bool _isHapticFeedbackActive = true;

    private bool _isInitialized = false;

    #region INITIALIZATION

    public override void Awake()
    {
        base.Awake();

        if (this != Instance) return;

        InitializeManagers();
    }

    private void Start()
    {
        if (this != Instance) return;

        StartCoroutine(InitializeUI());
    }

    private void OnEnable()
    {
        if (this != Instance) return;

        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(SafeSubscribeToCustomUpdate());
        SubscribeToUIEvents();
    }

    private void OnDisable()
    {
        if (this != Instance) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (CustomUpdateManager.Instance != null)
        {
            CustomUpdateManager.Instance.UnsubscribeFromUpdate(CustomUpdate);
        }

        UnsubscribeFromUIEvents();
    }

    private void InitializeManagers()
    {
        _buttonManager = GetComponent<ButtonManager>();
        _gameplayUIManager = GetComponent<GameplayUIManager>();
        _preGameUIManager = GetComponent<PreGameUIManager>();

        if (_dailyWheelUI == null)
        {
            _dailyWheelUI = GetComponentInChildren<DailyWheelUI>();
        }

        if (_buttonManager == null)
            Debug.LogError("[UIManager] ButtonManager no encontrado");
        if (_gameplayUIManager == null)
            Debug.LogError("[UIManager] GameplayUIManager no encontrado");
    }

    private IEnumerator InitializeUI()
    {
        yield return new WaitForEndOfFrame();

        while (SaveManager.Instance == null || !SaveManager.Instance.IsDataLoaded)
        {
            yield return null;
        }

        _buttonManager.SetupButtons();
        _gameplayUIManager.Initialize();

        _isInitialized = true;

        Debug.Log("[UIManager] Inicializacion completa");
    }

    private IEnumerator SafeSubscribeToCustomUpdate()
    {
        while (CustomUpdateManager.Instance == null)
        {
            yield return null;
        }

        CustomUpdateManager.Instance.SubscribeToUpdate(CustomUpdate);
    }

    private void CustomUpdate()
    {
        if (_isInitialized)
        {
            _gameplayUIManager.UpdateUI();

            if (Input.GetKeyDown(KeyCode.T))
            {
                Cursor.visible = !Cursor.visible;
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_gameplayUIManager != null)
            _gameplayUIManager.OnSceneLoaded();
    }

    #endregion

    #region EVENT SUBSCRIPTION

    private void SubscribeToUIEvents()
    {
        UIEvents.OnShowPauseOverlayRequested += ShowPauseOverlay;
        UIEvents.OnHidePauseOverlayRequested += HidePauseOverlay;
        UIEvents.OnTogglePauseOverlayRequested += TogglePauseOverlay;

        UIEvents.OnShowNoLivesOverlayRequested += ShowNoLivesOverlay;
        UIEvents.OnHideNoLivesOverlayRequested += HideNoLivesOverlay;

        UIEvents.OnShowLifeLostOverlayRequested += ShowLifeLostOverlay;

        UIEvents.OnShowSplashScreenRequested += ShowSplashScreen;
        UIEvents.OnHideSplashScreenRequested += HideSplashScreen;

        UIEvents.OnShowLevelsScreenRequested += ShowLevelsScreen;
        UIEvents.OnHideLevelsScreenRequested += HideLevelsScreen;

        UIEvents.OnShowPreGameScreenRequested += ShowPreGameScreen;
        UIEvents.OnHidePreGameScreenRequested += HidePreGameScreen;
        UIEvents.OnTogglePreGameScreenRequested += TogglePreGameScreen;

        UIEvents.OnShowCreditsModalRequested += ShowCreditsModal;
        UIEvents.OnHideCreditsModalRequested += HideCreditsModal;
        UIEvents.OnToggleCreditsModalRequested += ToggleCreditsModal;

        UIEvents.OnShowProfileModalRequested += ShowProfileModal;
        UIEvents.OnHideProfileModalRequested += HideProfileModal;
        UIEvents.OnToggleProfileModalRequested += ToggleProfileModal;

        UIEvents.OnShowDailyRewardModalRequested += ShowDailyRewardModal;
        UIEvents.OnHideDailyRewardModalRequested += HideDailyRewardModal;
        UIEvents.OnToggleDailyRewardModalRequested += ToggleDailyRewardModal;

        UIEvents.OnShowDailyWheelModalRequested += ShowDailyWheelModal;
        UIEvents.OnHideDailyWheelModalRequested += HideDailyWheelModal;
        UIEvents.OnToggleDailyWheelModalRequested += ToggleDailyWheelModal;

        UIEvents.OnShowStoreModalRequested += ShowStoreModal;
        UIEvents.OnHideStoreModalRequested += HideStoreModal;
        UIEvents.OnToggleStoreModalRequested += ToggleStoreModal;

        UIEvents.OnShowResultsModalRequested += ShowResultsModal;
        UIEvents.OnHideResultsModalRequested += HideResultsModal;
        UIEvents.OnToggleResultsModalRequested += ToggleResultsModal;

        UIEvents.OnShowGameplayHUDRequested += ShowGameplayHUD;
        UIEvents.OnHideGameplayHUDRequested += HideGameplayHUD;

        UIEvents.OnLevelPreviewRequested += HandleLevelPreviewRequested;

        UIEvents.OnLevelSelectorReady += OnLevelSelectorReady;

        if (_dailyWheelUI != null)
        {
            _dailyWheelUI.OnWheelProcessComplete += OnWheelCompleted;
        }
    }

    private void UnsubscribeFromUIEvents()
    {
        UIEvents.OnShowPauseOverlayRequested -= ShowPauseOverlay;
        UIEvents.OnHidePauseOverlayRequested -= HidePauseOverlay;
        UIEvents.OnTogglePauseOverlayRequested -= TogglePauseOverlay;

        UIEvents.OnShowNoLivesOverlayRequested -= ShowNoLivesOverlay;
        UIEvents.OnHideNoLivesOverlayRequested -= HideNoLivesOverlay;

        UIEvents.OnShowLifeLostOverlayRequested -= ShowLifeLostOverlay;

        UIEvents.OnShowSplashScreenRequested -= ShowSplashScreen;
        UIEvents.OnHideSplashScreenRequested -= HideSplashScreen;

        UIEvents.OnShowLevelsScreenRequested -= ShowLevelsScreen;
        UIEvents.OnHideLevelsScreenRequested -= HideLevelsScreen;

        UIEvents.OnShowPreGameScreenRequested -= ShowPreGameScreen;
        UIEvents.OnHidePreGameScreenRequested -= HidePreGameScreen;
        UIEvents.OnTogglePreGameScreenRequested -= TogglePreGameScreen;

        UIEvents.OnShowCreditsModalRequested -= ShowCreditsModal;
        UIEvents.OnHideCreditsModalRequested -= HideCreditsModal;
        UIEvents.OnToggleCreditsModalRequested -= ToggleCreditsModal;

        UIEvents.OnShowProfileModalRequested -= ShowProfileModal;
        UIEvents.OnHideProfileModalRequested -= HideProfileModal;
        UIEvents.OnToggleProfileModalRequested -= ToggleProfileModal;

        UIEvents.OnShowDailyRewardModalRequested -= ShowDailyRewardModal;
        UIEvents.OnHideDailyRewardModalRequested -= HideDailyRewardModal;
        UIEvents.OnToggleDailyRewardModalRequested -= ToggleDailyRewardModal;

        UIEvents.OnShowDailyWheelModalRequested -= ShowDailyWheelModal;
        UIEvents.OnHideDailyWheelModalRequested -= HideDailyWheelModal;
        UIEvents.OnToggleDailyWheelModalRequested -= ToggleDailyWheelModal;

        UIEvents.OnShowStoreModalRequested -= ShowStoreModal;
        UIEvents.OnHideStoreModalRequested -= HideStoreModal;
        UIEvents.OnToggleStoreModalRequested -= ToggleStoreModal;

        UIEvents.OnShowResultsModalRequested -= ShowResultsModal;
        UIEvents.OnHideResultsModalRequested -= HideResultsModal;
        UIEvents.OnToggleResultsModalRequested -= ToggleResultsModal;

        UIEvents.OnShowGameplayHUDRequested -= ShowGameplayHUD;
        UIEvents.OnHideGameplayHUDRequested -= HideGameplayHUD;

        UIEvents.OnLevelPreviewRequested -= HandleLevelPreviewRequested;

        UIEvents.OnLevelSelectorReady -= OnLevelSelectorReady;

        if (_dailyWheelUI != null)
        {
            _dailyWheelUI.OnWheelProcessComplete -= OnWheelCompleted;
        }
    }

    #endregion

    #region DAILY SEQUENCE

    private void OnLevelSelectorReady()
    {
        UIEvents.RequestUpdateLivesUI(LifeManager.Instance.CurrentLives);
        GetComponent<DebugUIManager>()?.ShowStarsDebug();
        StartCoroutine(CheckAndShowDailySequence());
    }

    private IEnumerator CheckAndShowDailySequence()
    {
        while (SaveManager.Instance == null || !SaveManager.Instance.IsDataLoaded ||
               DailyWheelSystem.Instance == null || DailyRewardSystem.Instance == null)
        {
            yield return null;
        }

        yield return null;

        bool canSpinWheel = DailyWheelSystem.Instance.CanSpinToday();
        bool canClaimReward = DailyRewardSystem.Instance.CanClaimToday();

        if (canSpinWheel)
        {
            yield return new WaitForSeconds(1f);
            ShowDailyWheelModal();
        }
        else if (canClaimReward)
        {
            yield return new WaitForSeconds(1f);
            ShowDailyRewardWithRefresh();
        }
    }

    private void OnWheelCompleted()
    {
        HideDailyWheelModal();
        StartCoroutine(CheckDailyRewardAfterWheel());
    }

    private IEnumerator CheckDailyRewardAfterWheel()
    {
        yield return new WaitForSeconds(0.5f);

        bool canClaimReward = DailyRewardSystem.Instance.CanClaimToday();

        if (canClaimReward)
        {
            yield return new WaitForSeconds(0.5f);
            ShowDailyRewardWithRefresh();
        }
    }

    private void ShowDailyRewardWithRefresh()
    {
        ShowDailyRewardModal();

        if (DailyRewardUIManager.Instance != null)
        {
            DailyRewardUIManager.Instance.ShowDailyReward();
        }
    }

    #endregion

    #region OVERLAYS

    private void ShowPauseOverlay() => ShowPanel(_pauseOverlay);
    private void HidePauseOverlay() => HidePanel(_pauseOverlay);
    public void TogglePauseOverlay() => TogglePanel(_pauseOverlay);

    private void ShowNoLivesOverlay() => ShowPanel(_noLivesOverlay);
    private void HideNoLivesOverlay() => HidePanel(_noLivesOverlay);

    private void ShowLifeLostOverlay(int livesRemaining)
    {
        if (_lifeLostOverlay != null)
            _lifeLostOverlay.ShowLifeLost(livesRemaining);
    }

    public void HideLifeLostOverlay()
    {
        if (_lifeLostOverlay != null)
            _lifeLostOverlay.Hide();
    }

    #endregion

    #region SCREENS

    private void ShowSplashScreen() => ShowPanel(_splashScreen);
    private void HideSplashScreen() => HidePanel(_splashScreen);

    private void ShowLevelsScreen() => ShowPanel(_levelsScreen);
    private void HideLevelsScreen() => HidePanel(_levelsScreen);

    private void ShowPreGameScreen() => ShowPanel(_preGameScreen);
    private void HidePreGameScreen() => HidePanel(_preGameScreen);
    private void TogglePreGameScreen() => TogglePanel(_preGameScreen);

    public void SetLevelsScreenEnabled(bool enabled)
    {
        if (enabled)
            ShowLevelsScreen();
        else
            HideLevelsScreen();
    }

    public void ResetLevelsScreenAnimation()
    {
        if (_levelsScreen != null)
            _levelsScreen.ResetAnimationFlag();
    }

    #endregion

    #region MODALS

    private void ShowCreditsModal() => ShowPanel(_creditsModal);
    private void HideCreditsModal() => HidePanel(_creditsModal);
    private void ToggleCreditsModal() => TogglePanel(_creditsModal);

    private void ShowProfileModal() => ShowPanel(_profileModal);
    private void HideProfileModal() => HidePanel(_profileModal);
    private void ToggleProfileModal() => TogglePanel(_profileModal);

    private void ShowDailyRewardModal() => ShowPanel(_dailyRewardModal);
    private void HideDailyRewardModal() => HidePanel(_dailyRewardModal);
    private void ToggleDailyRewardModal() => TogglePanel(_dailyRewardModal);
    public bool IsDailyRewardModalVisible() => IsPanelVisible(_dailyRewardModal);

    private void ShowDailyWheelModal() => ShowPanel(_dailyWheelModal);
    private void HideDailyWheelModal() => HidePanel(_dailyWheelModal);
    private void ToggleDailyWheelModal() => TogglePanel(_dailyWheelModal);

    private void ShowStoreModal() => ShowPanel(_storeModal);
    private void HideStoreModal() => HidePanel(_storeModal);
    private void ToggleStoreModal() => TogglePanel(_storeModal);

    private void ShowResultsModal() => ShowPanel(_resultsModal);
    private void HideResultsModal() => HidePanel(_resultsModal);
    private void ToggleResultsModal() => TogglePanel(_resultsModal);

    #endregion

    #region HUD

    private void ShowGameplayHUD() => ShowPanel(_gameplayHUD);
    private void HideGameplayHUD() => HidePanel(_gameplayHUD);
    public void SetGameplayHUDEnabled(bool enabled)
    {
        if (enabled)
            ShowGameplayHUD();
        else
            HideGameplayHUD();
    }

    #endregion

    #region LEVEL PREVIEW

    private void HandleLevelPreviewRequested(string sceneName)
    {
        if (_preGameUIManager != null)
        {
            _preGameUIManager.ShowConfirmationPanel(sceneName);
        }
    }

    #endregion

    #region UTILITY METHODS

    private void ShowPanel(UIPanel panel)
    {
        if (panel == null) return;
        panel.Show();
    }

    private void HidePanel(UIPanel panel)
    {
        if (panel == null) return;
        panel.Hide();
    }

    private void TogglePanel(UIPanel panel)
    {
        if (panel == null) return;

        if (panel.IsVisible)
            panel.Hide();
        else
            panel.Show();
    }

    private bool IsPanelVisible(UIPanel panel)
    {
        return panel != null && panel.IsVisible;
    }

    #endregion

    /*#region LEGACY PUBLIC METHODS - DEPRECATED

    [Obsolete("Usa UIEvents.RequestTogglePauseOverlay() en su lugar")]
    public void ShowHidePauseCanvas() => UIEvents.RequestTogglePauseOverlay();

    [Obsolete("Usa UIEvents.RequestTogglePreGameScreen() en su lugar")]
    public void ShowHidePreGameCanvas() => UIEvents.RequestTogglePreGameScreen();

    [Obsolete("Usa UIEvents.RequestToggleCreditsModal() en su lugar")]
    public void ShowHideCreditsCanvas() => UIEvents.RequestToggleCreditsModal();

    [Obsolete("Usa UIEvents.RequestToggleProfileModal() en su lugar")]
    public void ShowHideProfileCanvas() => UIEvents.RequestToggleProfileModal();

    [Obsolete("Usa UIEvents.RequestToggleResultsModal() en su lugar")]
    public void ShowHideResultsCanvas() => UIEvents.RequestToggleResultsModal();

    [Obsolete("Usa UIEvents.RequestToggleStoreModal() en su lugar")]
    public void ShowHideStoreCanvas() => UIEvents.RequestToggleStoreModal();

    [Obsolete("Usa UIEvents.RequestToggleDailyWheelModal() en su lugar")]
    public void ShowHideDailyWheelCanvas() => UIEvents.RequestToggleDailyWheelModal();

    [Obsolete("Usa UIEvents.RequestShowDailyRewardModal() en su lugar")]
    public void ShowHideDailyRewardCanvas() => UIEvents.RequestShowDailyRewardModal();

    [Obsolete("Usa UIEvents.RequestShowNoLivesOverlay() en su lugar")]
    public void ShowHideNoLivesCanvas() => UIEvents.RequestShowNoLivesOverlay();

    [Obsolete("Usa UIEvents.RequestShowLifeLostOverlay(lives) en su lugar")]
    public void ShowHideLifeLostCanvas() => UIEvents.RequestShowLifeLostOverlay(LifeManager.Instance?.CurrentLives ?? 0);

    [Obsolete("Usa UIEvents.RequestLevelPreview(sceneName) en su lugar")]
    public void ShowConfirmationPanel(string sceneName) => UIEvents.RequestLevelPreview(sceneName);

    [Obsolete("Usa UIEvents.RequestUpdateLivesUI(lives) en su lugar")]
    public void UpdateLivesUI(int lives) => UIEvents.RequestUpdateLivesUI(lives);

    [Obsolete("Usa UIEvents.RequestShowNoLivesPanel() en su lugar")]
    public void ShowNoLivesPanel() => UIEvents.RequestShowNoLivesPanel();

    [Obsolete("Llama directamente a SwitchHapticFeedback()")]
    public void SwitchHapticFeedback() => _isHapticFeedbackActive = !_isHapticFeedbackActive;

    [Obsolete("Usa UIEvents.RequestShowLevelSelector() en su lugar")]
    public void ShowLevelSelector() => UIEvents.RequestShowLevelSelector();

    [Obsolete("Usa UIEvents.RequestSceneTransition(sceneName) en su lugar")]
    public void LoadLevelScene(string sceneName) => UIEvents.RequestSceneTransition(sceneName);

    [Obsolete("Usa UIEvents.RequestRestartLevel() en su lugar")]
    public void RestartLevel() => UIEvents.RequestRestartLevel();

    public void OpenURL(string url) => Application.OpenURL(url);

    #endregion*/
}