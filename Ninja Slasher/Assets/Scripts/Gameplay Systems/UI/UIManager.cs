using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using AstroRift.Core.Update;

public class UIManager : MonoBehaviourSingleton<UIManager>
{
    [Header("MANAGERS")]
    private CanvasManager _canvasManager;
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

    public override void Awake()
    {
        base.Awake();

        if (this != Instance)
        {
            return;
        }

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

    private IEnumerator InitializeUI()
    {
        yield return new WaitForEndOfFrame();

        while (SaveManager.Instance == null ||
               !SaveManager.Instance.IsDataLoaded)
        {
            yield return null;
        }

        _buttonManager.SetupButtons();
        _gameplayUIManager.Initialize();

        _isInitialized = true;

        Debug.Log("[UIManager] Inicializacion completa");
    }

    private void InitializeManagers()
    {
        _canvasManager = GetComponent<CanvasManager>();
        _buttonManager = GetComponent<ButtonManager>();
        _gameplayUIManager = GetComponent<GameplayUIManager>();
        _preGameUIManager = GetComponent<PreGameUIManager>();
        _sceneTransitionManager = GetComponent<SceneTransitionManager>();

        if (_dailyWheelUI == null)
        {
            _dailyWheelUI = GetComponentInChildren<DailyWheelUI>();
        }

        if (_canvasManager == null)
            Debug.LogError("[UIManager] CanvasManager no encontrado");
        if (_buttonManager == null)
            Debug.LogError("[UIManager] ButtonManager no encontrado");
        if (_gameplayUIManager == null)
            Debug.LogError("[UIManager] GameplayUIManager no encontrado");
    }

    private IEnumerator SafeSubscribeToCustomUpdate()
    {
        while (CustomUpdateManager.Instance == null)
        {
            yield return null;
        }

        CustomUpdateManager.Instance.SubscribeToUpdate(CustomUpdate);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_gameplayUIManager != null)
            _gameplayUIManager.OnSceneLoaded();
    }

    public void OpenURL(string url)
    {
        Application.OpenURL(url);
    }

    private void SubscribeToUIEvents()
    {
        UIEvents.OnLevelSelectorReady += OnLevelSelectorReady;

        if (_dailyWheelUI != null)
        {
            _dailyWheelUI.OnWheelProcessComplete += OnWheelCompleted;
        }

        /*TO DO: Migrar a UIEvents
        UIEvents.OnPanelOpenRequested += HandlePanelOpenRequest;
        UIEvents.OnPanelCloseRequested += HandlePanelCloseRequest;
        UIEvents.OnSceneTransitionRequested += HandleSceneTransition;
        */
    }

    private void UnsubscribeFromUIEvents()
    {
        UIEvents.OnLevelSelectorReady -= OnLevelSelectorReady;

        if (_dailyWheelUI != null)
        {
            _dailyWheelUI.OnWheelProcessComplete -= OnWheelCompleted;
        }

        /*TO DO: Migrar a UIEvents
        UIEvents.OnPanelOpenRequested -= HandlePanelOpenRequest;
        UIEvents.OnPanelCloseRequested -= HandlePanelCloseRequest;
        UIEvents.OnSceneTransitionRequested -= HandleSceneTransition;
        */
    }

    //private void HandlePanelOpenRequest(string panelName)
    //{
    //    // Logica para abrir paneles por nombre
    //}

    //private void HandlePanelCloseRequest(string panelName)
    //{
    //    // Logica para cerrar paneles por nombre
    //}

    //private void HandleSceneTransition(string sceneName)
    //{
    //    _sceneTransitionManager.LoadLevelScene(sceneName);
    //}

    #region DAILY SEQUENCE (UI Logic Only)

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

    public void ShowPauseOverlay()
    {
        if (_pauseOverlay != null)
            _pauseOverlay.Show();
    }

    public void HidePauseOverlay()
    {
        if (_pauseOverlay != null)
            _pauseOverlay.Hide();
    }

    public void TogglePauseOverlay()
    {
        if (_pauseOverlay == null) return;

        if (_pauseOverlay.IsVisible)
            _pauseOverlay.Hide();
        else
            _pauseOverlay.Show();
    }

    public void ShowNoLivesOverlay()
    {
        if (_noLivesOverlay != null)
            _noLivesOverlay.Show();
    }

    public void HideNoLivesOverlay()
    {
        if (_noLivesOverlay != null)
            _noLivesOverlay.Hide();
    }

    public void ShowLifeLostOverlay(int livesRemaining)
    {
        if (_lifeLostOverlay != null)
            _lifeLostOverlay.ShowLifeLost(livesRemaining);
    }

    #endregion

    #region SCREENS

    public void ShowSplashScreen()
    {
        if (_splashScreen != null)
            _splashScreen.Show();
    }

    public void HideSplashScreen()
    {
        if (_splashScreen != null)
            _splashScreen.Hide();
    }

    public void ShowPreGameScreen()
    {
        if (_preGameScreen != null)
            _preGameScreen.Show();
    }

    public void HidePreGameScreen()
    {
        if (_preGameScreen != null)
            _preGameScreen.Hide();
    }

    public void TogglePreGameScreen()
    {
        if (_preGameScreen == null) return;

        if (_preGameScreen.IsVisible)
            _preGameScreen.Hide();
        else
            _preGameScreen.Show();
    }

    #endregion

    #region MODALS

    public void ShowCreditsModal()
    {
        if (_creditsModal != null)
            _creditsModal.Show();
    }

    public void HideCreditsModal()
    {
        if (_creditsModal != null)
            _creditsModal.Hide();
    }

    public void ShowProfileModal()
    {
        if (_profileModal != null)
            _profileModal.Show();
    }

    public void HideProfileModal()
    {
        if (_profileModal != null)
            _profileModal.Hide();
    }

    public void ToggleProfileModal()
    {
        if (_profileModal == null) return;

        if (_profileModal.IsVisible)
            _profileModal.Hide();
        else
            _profileModal.Show();
    }

    public void ShowDailyRewardModal()
    {
        if (_dailyRewardModal != null)
            _dailyRewardModal.Show();
    }

    public void HideDailyRewardModal()
    {
        if (_dailyRewardModal != null)
            _dailyRewardModal.Hide();
    }

    public void ToggleDailyRewardModal()
    {
        if (_dailyRewardModal == null) return;

        if (_dailyRewardModal.IsVisible)
            _dailyRewardModal.Hide();
        else
            _dailyRewardModal.Show();
    }

    public bool IsDailyRewardModalVisible()
    {
        return _dailyRewardModal != null && _dailyRewardModal.IsVisible;
    }

    public void ShowDailyWheelModal()
    {
        if (_dailyWheelModal != null)
            _dailyWheelModal.Show();
    }

    public void HideDailyWheelModal()
    {
        if (_dailyWheelModal != null)
            _dailyWheelModal.Hide();
    }

    public void ToggleDailyWheelModal()
    {
        if (_dailyWheelModal == null) return;

        if (_dailyWheelModal.IsVisible)
            _dailyWheelModal.Hide();
        else
            _dailyWheelModal.Show();
    }

    public bool IsDailyWheelModalVisible()
    {
        return _dailyWheelModal != null && _dailyWheelModal.IsVisible;
    }

    public void ShowStoreModal()
    {
        if (_storeModal != null)
            _storeModal.Show();
    }

    public void HideStoreModal()
    {
        if (_storeModal != null)
            _storeModal.Hide();
    }

    public void ToggleStoreModal()
    {
        if (_storeModal == null) return;

        if (_storeModal.IsVisible)
            _storeModal.Hide();
        else
            _storeModal.Show();
    }

    public bool IsStoreModalVisible()
    {
        return _storeModal != null && _storeModal.IsVisible;
    }

    public void ShowResultsModal()
    {
        if (_resultsModal != null)
            _resultsModal.Show();
    }

    public void HideResultsModal()
    {
        if (_resultsModal != null)
            _resultsModal.Hide();
    }

    public void ToggleResultsModal()
    {
        if (_resultsModal == null) return;

        if (_resultsModal.IsVisible)
            _resultsModal.Hide();
        else
            _resultsModal.Show();
    }

    public bool IsResultsModalVisible()
    {
        return _resultsModal != null && _resultsModal.IsVisible;
    }

    #endregion

    #region HUD

    public void ShowGameplayHUD()
    {
        if (_gameplayHUD != null)
            _gameplayHUD.Show();
    }

    public void HideGameplayHUD()
    {
        if (_gameplayHUD != null)
            _gameplayHUD.Hide();
    }

    public void SetGameplayHUDEnabled(bool enabled)
    {
        if (_gameplayHUD == null) return;

        if (enabled)
            _gameplayHUD.Show();
        else
            _gameplayHUD.Hide();
    }

    #endregion

    #region LEGACY

    public void ShowConfirmationPanel(string sceneName) => _preGameUIManager.ShowConfirmationPanel(sceneName);
    public void ShowHidePreGameCanvas()
    {
        if (_preGameScreen != null)
            TogglePreGameScreen();
    }

    public void ShowHideCreditsCanvas()
    {
        if (_creditsModal == null) return;

        if (_creditsModal.IsVisible)
            _creditsModal.Hide();
        else
            _creditsModal.Show();
    }

    public void ShowHideProfileCanvas()
    {
        if (_profileModal != null)
            ToggleProfileModal();
    }

    public void SwitchHapticFeedback() => _isHapticFeedbackActive = !_isHapticFeedbackActive;
    public void ShowHidePauseCanvas() => TogglePauseOverlay();
    public void ShowHideResultsCanvas()
    {
        if (_resultsModal != null)
            ToggleResultsModal();
    }

    public void ShowHideLifeLostCanvas() => ShowLifeLostOverlay(LifeManager.Instance?.CurrentLives ?? 0);
    public void ShowHideDailyRewardCanvas() => ShowDailyRewardModal();
    public void ShowHideNoLivesCanvas() => ShowNoLivesOverlay();
    public void UpdateLivesUI(int lives) => _gameplayUIManager.UpdateLivesUI(lives);
    public void ShowNoLivesPanel() => _gameplayUIManager.ShowNoLivesPanel();
    public void ShowHideStoreCanvas()
    {
        if (_storeModal != null)
            ToggleStoreModal();
    }

    public void ShowHideDailyWheelCanvas()
    {
        if (_dailyWheelModal != null)
            ToggleDailyWheelModal();
    }

    public void ShowLevelSelector()
    {
        UIEvents.RequestShowLevelSelector();
    }

    public void LoadLevelScene(string sceneName)
    {
        UIEvents.RequestSceneTransition(sceneName);
    }

    public void RestartLevel()
    {
        UIEvents.RequestRestartLevel();
    }

    #endregion
}