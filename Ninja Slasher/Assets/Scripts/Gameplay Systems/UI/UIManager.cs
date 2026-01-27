using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using AstroRift.Core.Update;

public class UIManager : MonoBehaviourSingleton<UIManager>
{
    [Header("MANAGERS")]
    private ButtonManager _buttonManager;
    private GameplayUIManager _gameplayUIManager;
    private PreGameUIManager _preGameUIManager;
    private SceneTransitionManager _sceneTransitionManager;

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
        _sceneTransitionManager = GetComponent<SceneTransitionManager>();

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

    #region EVENT MANAGEMENT

    private void SubscribeToUIEvents()
    {
        UIEvents.OnLevelSelectorReady += OnLevelSelectorReady;

        if (_dailyWheelUI != null)
        {
            _dailyWheelUI.OnWheelProcessComplete += OnWheelCompleted;
        }
    }

    private void UnsubscribeFromUIEvents()
    {
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

    #region GENERIC PANEL METHODS

    private void ShowPanel(UIPanel panel)
    {
        if (panel != null)
            panel.Show();
    }

    private void HidePanel(UIPanel panel)
    {
        if (panel != null)
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

    #region OVERLAYS

    public void ShowPauseOverlay() => ShowPanel(_pauseOverlay);
    public void HidePauseOverlay() => HidePanel(_pauseOverlay);
    public void TogglePauseOverlay() => TogglePanel(_pauseOverlay);

    public void ShowNoLivesOverlay() => ShowPanel(_noLivesOverlay);
    public void HideNoLivesOverlay() => HidePanel(_noLivesOverlay);

    public void ShowLifeLostOverlay(int livesRemaining)
    {
        if (_lifeLostOverlay != null)
            _lifeLostOverlay.ShowLifeLost(livesRemaining);
    }

    #endregion

    #region SCREENS

    public void ShowSplashScreen() => ShowPanel(_splashScreen);
    public void HideSplashScreen() => HidePanel(_splashScreen);

    public void ShowLevelsScreen() => ShowPanel(_levelsScreen);
    public void HideLevelsScreen() => HidePanel(_levelsScreen);

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

    public void ShowPreGameScreen() => ShowPanel(_preGameScreen);
    public void HidePreGameScreen() => HidePanel(_preGameScreen);
    public void TogglePreGameScreen() => TogglePanel(_preGameScreen);

    #endregion

    #region MODALS

    public void ShowCreditsModal() => ShowPanel(_creditsModal);
    public void HideCreditsModal() => HidePanel(_creditsModal);
    public void ToggleCreditsModal() => TogglePanel(_creditsModal);

    public void ShowProfileModal() => ShowPanel(_profileModal);
    public void HideProfileModal() => HidePanel(_profileModal);
    public void ToggleProfileModal() => TogglePanel(_profileModal);

    public void ShowDailyRewardModal() => ShowPanel(_dailyRewardModal);
    public void HideDailyRewardModal() => HidePanel(_dailyRewardModal);
    public void ToggleDailyRewardModal() => TogglePanel(_dailyRewardModal);
    public bool IsDailyRewardModalVisible() => IsPanelVisible(_dailyRewardModal);

    public void ShowDailyWheelModal() => ShowPanel(_dailyWheelModal);
    public void HideDailyWheelModal() => HidePanel(_dailyWheelModal);
    public void ToggleDailyWheelModal() => TogglePanel(_dailyWheelModal);
    public bool IsDailyWheelModalVisible() => IsPanelVisible(_dailyWheelModal);

    public void ShowStoreModal() => ShowPanel(_storeModal);
    public void HideStoreModal() => HidePanel(_storeModal);
    public void ToggleStoreModal() => TogglePanel(_storeModal);
    public bool IsStoreModalVisible() => IsPanelVisible(_storeModal);

    public void ShowResultsModal() => ShowPanel(_resultsModal);
    public void HideResultsModal() => HidePanel(_resultsModal);
    public void ToggleResultsModal() => TogglePanel(_resultsModal);
    public bool IsResultsModalVisible() => IsPanelVisible(_resultsModal);

    #endregion

    #region HUD

    public void ShowGameplayHUD() => ShowPanel(_gameplayHUD);
    public void HideGameplayHUD() => HidePanel(_gameplayHUD);

    public void SetGameplayHUDEnabled(bool enabled)
    {
        if (enabled)
            ShowGameplayHUD();
        else
            HideGameplayHUD();
    }

    #endregion

    #region LEGACY METHODS

    public void ShowConfirmationPanel(string sceneName) => _preGameUIManager.ShowConfirmationPanel(sceneName);
    public void ShowHidePreGameCanvas() => TogglePreGameScreen();
    public void ShowHideCreditsCanvas() => ToggleCreditsModal();
    public void ShowHideProfileCanvas() => ToggleProfileModal();
    public void ShowHidePauseCanvas() => TogglePauseOverlay();
    public void ShowHideResultsCanvas() => ToggleResultsModal();
    public void ShowHideStoreCanvas() => ToggleStoreModal();
    public void ShowHideDailyWheelCanvas() => ToggleDailyWheelModal();
    public void ShowHideDailyRewardCanvas() => ShowDailyRewardModal();
    public void ShowHideNoLivesCanvas() => ShowNoLivesOverlay();
    public void ShowHideLifeLostCanvas() => ShowLifeLostOverlay(LifeManager.Instance?.CurrentLives ?? 0);

    public void UpdateLivesUI(int lives) => _gameplayUIManager.UpdateLivesUI(lives);
    public void ShowNoLivesPanel() => _gameplayUIManager.ShowNoLivesPanel();

    public void SwitchHapticFeedback() => _isHapticFeedbackActive = !_isHapticFeedbackActive;

    public void ShowLevelSelector() => UIEvents.RequestShowLevelSelector();
    public void LoadLevelScene(string sceneName) => UIEvents.RequestSceneTransition(sceneName);
    public void RestartLevel() => UIEvents.RequestRestartLevel();

    public void OpenURL(string url) => Application.OpenURL(url);

    #endregion
}