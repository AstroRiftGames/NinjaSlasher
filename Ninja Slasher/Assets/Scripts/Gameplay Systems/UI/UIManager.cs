using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviourSingleton<UIManager>
{
    [Header("MANAGERS")]
    private ButtonManager _buttonManager;
    private GameplayUIManager _gameplayUIManager;
    private PreGameUIManager _preGameUIManager;
    private SceneTransitionManager _sceneTransitionManager;

    [Header("OVERLAYS")]
    [SerializeField] private PauseOverlay _pauseOverlay;
    [SerializeField] private TutorialUIOverlay _tutorialOverlay;

    [Header("SCREENS")]
    [SerializeField] private SplashScreen _splashScreen;
    [SerializeField] private LevelsScreen _levelsScreen;

    [Header("MODALS")]
    [SerializeField] private PregameModal _pregameModal;
    [SerializeField] private NoLivesModal _noLivesModal;
    [SerializeField] private DefeatModal _defeatModal;
    [SerializeField] private EmergencyBundleModal _emergencyBundleModal;
    [SerializeField] private CreditsModal _creditsModal;
    [SerializeField] private ProfileModal _profileModal;
    [SerializeField] private DailyRewardModal _dailyRewardModal;
    [SerializeField] private DailyWheelModal _dailyWheelModal;
    [SerializeField] private StoreModal _storeModal;
    [SerializeField] private VictoryModal _victoryModal;

    [Header("HUD")]
    [SerializeField] private GameplayHUD _gameplayHUD;

    public bool IsHapticFeedbackActive => _isHapticFeedbackActive;
    private bool _isHapticFeedbackActive = true;

    private bool _isInitialized = false;
    private bool _shouldGameplayHUDBeVisible;
    private readonly List<UIModalBase> _activeModals = new();
    private bool _uiRequestLock;
    private Coroutine _uiFlowRoutine;

    public bool IsUIBusy => _uiRequestLock || _uiFlowRoutine != null;


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
        UIPanel.OnBlockingPanelVisibilityChanged += RefreshGameplayHUDVisibility;
        StartCoroutine(SafeSubscribeToCustomUpdate());
        SubscribeToUIEvents();
        SubscribeToGameEvents();
        RefreshGameplayHUDVisibility();
    }

    private void OnDisable()
    {
        if (this != Instance) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        UIPanel.OnBlockingPanelVisibilityChanged -= RefreshGameplayHUDVisibility;

        if (CustomUpdateManager.Instance != null)
        {
            CustomUpdateManager.Instance.UnsubscribeFromUpdate(CustomUpdate);
        }

        UnsubscribeFromUIEvents();
        UnsubscribeFromGameEvents();

        if (_uiFlowRoutine != null)
        {
            StopCoroutine(_uiFlowRoutine);
            _uiFlowRoutine = null;
        }

        _uiRequestLock = false;
    }

    private void InitializeManagers()
    {
        _buttonManager = GetComponent<ButtonManager>();
        _gameplayUIManager = GetComponent<GameplayUIManager>();
        _preGameUIManager = GetComponent<PreGameUIManager>();
        _sceneTransitionManager = GetComponentInChildren<SceneTransitionManager>(true);
        ResolveTutorialOverlay();

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

    private void Update()
    {
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleAndroidBack();
        }
#endif
    }

    private void HandleAndroidBack()
    {
        if (_emergencyBundleModal != null && _emergencyBundleModal.IsVisible)
        {
            UIEvents.RequestHideEmergencyBundleModal();
            return;
        }

        if (_activeModals.Count > 0)
        {
            UIModalBase top = _activeModals[_activeModals.Count - 1];
            if (top != _victoryModal && top != _defeatModal && top != _noLivesModal)
            {
                CloseModal(top);
                return;
            }
        }

        if (_pauseOverlay != null && _pauseOverlay.IsVisible)
        {
            HidePauseOverlay();
            return;
        }

        if (LevelSessionManager.Instance != null && LevelSessionManager.Instance.IsSessionRunning)
        {
            ShowPauseOverlay();
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

        UIEvents.OnShowTutorialOverlayRequested += ShowTutorialOverlay;
        UIEvents.OnHideTutorialOverlayRequested += HideTutorialOverlay;

        UIEvents.OnShowSplashScreenRequested += ShowSplashScreen;
        UIEvents.OnHideSplashScreenRequested += HideSplashScreen;

        UIEvents.OnShowLevelsScreenRequested += ShowLevelsScreen;
        UIEvents.OnHideLevelsScreenRequested += HideLevelsScreen;

        UIEvents.OnShowPregameModalRequested += ShowPregameModal;
        UIEvents.OnHidePregameModalRequested += HidePregameModal;
        UIEvents.OnTogglePregameModalRequested += TogglePregameModal;

        UIEvents.OnShowNoLivesModalRequested += ShowNoLivesModal;
        UIEvents.OnHideNoLivesModalRequested += HideNoLivesModal;

        UIEvents.OnShowDefeatModalRequested += ShowDefeatModal;
        UIEvents.OnHideDefeatModalRequested += HideDefeatModal;

        UIEvents.OnShowEmergencyBundleModalRequested += ShowEmergencyBundleModal;
        UIEvents.OnHideEmergencyBundleModalRequested += HideEmergencyBundleModal;

        UIEvents.OnShowCreditsModalRequested += ShowCreditsModal;
        UIEvents.OnHideCreditsModalRequested += HideCreditsModal;
        UIEvents.OnToggleCreditsModalRequested += ToggleCreditsModal;

        UIEvents.OnShowProfileModalRequested += ShowProfileModal;
        UIEvents.OnHideProfileModalRequested += HideProfileModal;
        UIEvents.OnToggleProfileModalRequested += ToggleProfileModal;

        UIEvents.OnShowDailyRewardModalRequested += ShowDailyRewardModal;
        UIEvents.OnHideDailyRewardModalRequested += OnHideDailyRewardRequested;
        UIEvents.OnToggleDailyRewardModalRequested += ToggleDailyRewardModal;

        UIEvents.OnShowDailyWheelModalRequested += ShowDailyWheelModal;
        UIEvents.OnHideDailyWheelModalRequested += OnHideDailyWheelRequested;
        UIEvents.OnToggleDailyWheelModalRequested += ToggleDailyWheelModal;

        UIEvents.OnShowStoreModalRequested += ShowStoreModal;
        UIEvents.OnHideStoreModalRequested += HideStoreModal;
        UIEvents.OnToggleStoreModalRequested += ToggleStoreModal;

        UIEvents.OnShowVictoryModalRequested += ShowVictoryModal;
        UIEvents.OnHideVictoryModalRequested += HideVictoryModal;
        UIEvents.OnToggleVictoryModalRequested += ToggleVictoryModal;

        UIEvents.OnShowGameplayHUDRequested += ShowGameplayHUD;
        UIEvents.OnHideGameplayHUDRequested += HideGameplayHUD;

        UIEvents.OnLevelPreviewRequested += HandleLevelPreviewRequested;
    }

    private void SubscribeToGameEvents()
    {
        GameEvents.OnLevelStarted += OnLevelStarted;
        GameEvents.OnLevelResultReady += OnLevelResultReady;
    }

    private void UnsubscribeFromUIEvents()
    {
        UIEvents.OnShowPauseOverlayRequested -= ShowPauseOverlay;
        UIEvents.OnHidePauseOverlayRequested -= HidePauseOverlay;
        UIEvents.OnTogglePauseOverlayRequested -= TogglePauseOverlay;

        UIEvents.OnShowTutorialOverlayRequested -= ShowTutorialOverlay;
        UIEvents.OnHideTutorialOverlayRequested -= HideTutorialOverlay;

        UIEvents.OnShowSplashScreenRequested -= ShowSplashScreen;
        UIEvents.OnHideSplashScreenRequested -= HideSplashScreen;

        UIEvents.OnShowLevelsScreenRequested -= ShowLevelsScreen;
        UIEvents.OnHideLevelsScreenRequested -= HideLevelsScreen;

        UIEvents.OnShowPregameModalRequested -= ShowPregameModal;
        UIEvents.OnHidePregameModalRequested -= HidePregameModal;
        UIEvents.OnTogglePregameModalRequested -= TogglePregameModal;

        UIEvents.OnShowNoLivesModalRequested -= ShowNoLivesModal;
        UIEvents.OnHideNoLivesModalRequested -= HideNoLivesModal;

        UIEvents.OnShowDefeatModalRequested -= ShowDefeatModal;
        UIEvents.OnHideDefeatModalRequested -= HideDefeatModal;

        UIEvents.OnShowEmergencyBundleModalRequested -= ShowEmergencyBundleModal;
        UIEvents.OnHideEmergencyBundleModalRequested -= HideEmergencyBundleModal;

        UIEvents.OnShowCreditsModalRequested -= ShowCreditsModal;
        UIEvents.OnHideCreditsModalRequested -= HideCreditsModal;
        UIEvents.OnToggleCreditsModalRequested -= ToggleCreditsModal;

        UIEvents.OnShowProfileModalRequested -= ShowProfileModal;
        UIEvents.OnHideProfileModalRequested -= HideProfileModal;
        UIEvents.OnToggleProfileModalRequested -= ToggleProfileModal;

        UIEvents.OnShowDailyRewardModalRequested -= ShowDailyRewardModal;
        UIEvents.OnHideDailyRewardModalRequested -= OnHideDailyRewardRequested;
        UIEvents.OnToggleDailyRewardModalRequested -= ToggleDailyRewardModal;

        UIEvents.OnShowDailyWheelModalRequested -= ShowDailyWheelModal;
        UIEvents.OnHideDailyWheelModalRequested -= OnHideDailyWheelRequested;
        UIEvents.OnToggleDailyWheelModalRequested -= ToggleDailyWheelModal;

        UIEvents.OnShowStoreModalRequested -= ShowStoreModal;
        UIEvents.OnHideStoreModalRequested -= HideStoreModal;
        UIEvents.OnToggleStoreModalRequested -= ToggleStoreModal;

        UIEvents.OnShowVictoryModalRequested -= ShowVictoryModal;
        UIEvents.OnHideVictoryModalRequested -= HideVictoryModal;
        UIEvents.OnToggleVictoryModalRequested -= ToggleVictoryModal;

        UIEvents.OnShowGameplayHUDRequested -= ShowGameplayHUD;
        UIEvents.OnHideGameplayHUDRequested -= HideGameplayHUD;

        UIEvents.OnLevelPreviewRequested -= HandleLevelPreviewRequested;
    }

    private void UnsubscribeFromGameEvents()
    {
        GameEvents.OnLevelStarted -= OnLevelStarted;
        GameEvents.OnLevelResultReady -= OnLevelResultReady;
    }

    #endregion

    private void OnLevelStarted()
    {
        ShowGameplayHUD();
    }

    private void OnLevelResultReady(LevelResult result)
    {
        HideGameplayHUD();

        switch (result)
        {
            case LevelResult.Victory:
                ShowVictoryModal();
                break;
            case LevelResult.NoLives:
                ShowNoLivesModal();
                break;
            case LevelResult.Defeat:
                ShowDefeatModal(LifeManager.Instance != null ? LifeManager.Instance.GetRealLives() : 0);
                break;
        }
    }

    #region OVERLAYS

    private void ShowPauseOverlay() => ShowPanel(_pauseOverlay);
    private void HidePauseOverlay() => HidePanel(_pauseOverlay);
    public void TogglePauseOverlay() => TogglePanel(_pauseOverlay);

    private void ShowTutorialOverlay() => ShowPanel(ResolveTutorialOverlay());
    private void HideTutorialOverlay() => HidePanel(ResolveTutorialOverlay());

    #endregion

    #region SCREENS

    private void ShowSplashScreen() => ShowPanel(_splashScreen);
    private void HideSplashScreen() => HidePanel(_splashScreen);

    private void ShowLevelsScreen() => ShowPanel(_levelsScreen);
    private void HideLevelsScreen() => HidePanel(_levelsScreen);

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
            _levelsScreen.ResetAnimationStateForScreenReturn();
    }

    public bool ShouldRunLevelSelectionStartupFlowOnNextEntry()
    {
        if (_sceneTransitionManager == null)
            _sceneTransitionManager = GetComponentInChildren<SceneTransitionManager>(true);

        return _sceneTransitionManager != null
            && _sceneTransitionManager.ShouldRunLevelSelectionStartupFlowOnNextEntry;
    }

    #endregion

    #region MODALS

    private void ShowPregameModal() => ShowModal(_pregameModal);
    private void HidePregameModal() => CloseModal(_pregameModal);
    private void TogglePregameModal() => ToggleModal(_pregameModal);

    private void ShowNoLivesModal()
    {
        StartManagedUIFlow(ShowNoLivesModalRoutine());
    }

    private void HideNoLivesModal() => CloseModal(_noLivesModal);

    private void ShowDefeatModal(int livesRemaining)
    {
        StartManagedUIFlow(ShowDefeatModalRoutine(livesRemaining));
    }

    private void HideDefeatModal() => CloseModal(_defeatModal);

    private void ShowEmergencyBundleModal(EmergencyBundleOffer offer)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[UIManager] ShowEmergencyBundleModal | modal assigned={_emergencyBundleModal != null}");
#endif
        StartManagedUIFlow(ShowEmergencyBundleModalRoutine(offer));
    }

    private void HideEmergencyBundleModal() => CloseModal(_emergencyBundleModal);

    private void ShowCreditsModal() => ShowModal(_creditsModal);
    private void HideCreditsModal() => CloseModal(_creditsModal);
    private void ToggleCreditsModal() => ToggleModal(_creditsModal);

    private void ShowProfileModal() => ShowModal(_profileModal);
    private void HideProfileModal() => CloseModal(_profileModal);
    private void ToggleProfileModal() => ToggleModal(_profileModal);

    private void ShowDailyRewardModal()
    {
        ShowModal(_dailyRewardModal);
        DailyRewardUIManager.Instance?.ShowDailyReward();
    }
    private void HideDailyRewardModal() => CloseModal(_dailyRewardModal);
    private void ToggleDailyRewardModal() => ToggleModal(_dailyRewardModal);
    public bool IsDailyRewardModalVisible() => IsModalVisible(_dailyRewardModal);

    private void OnHideDailyRewardRequested()
    {
        HideDailyRewardModal();
    }

    private void ShowDailyWheelModal() => ShowModal(_dailyWheelModal);
    private void HideDailyWheelModal() => CloseModal(_dailyWheelModal);
    private void ToggleDailyWheelModal() => ToggleModal(_dailyWheelModal);
    public bool IsDailyWheelModalVisible() => IsModalVisible(_dailyWheelModal);
    private void OnHideDailyWheelRequested()
    {
        HideDailyWheelModal();
    }

    private void ShowStoreModal() => ShowModal(_storeModal);
    private void HideStoreModal() => CloseModal(_storeModal);
    private void ToggleStoreModal() => ToggleModal(_storeModal);

    private void ShowVictoryModal() => ShowModal(_victoryModal);
    private void HideVictoryModal() => CloseModal(_victoryModal);
    private void ToggleVictoryModal() => ToggleModal(_victoryModal);

    #endregion

    #region HUD

    private void ShowGameplayHUD()
    {
        _shouldGameplayHUDBeVisible = true;
        RefreshGameplayHUDVisibility();
    }

    private void HideGameplayHUD()
    {
        _shouldGameplayHUDBeVisible = false;
        ApplyGameplayHUDVisibility(force: true);
    }

    public void SetGameplayHUDEnabled(bool enabled)
    {
        if (enabled)
            ShowGameplayHUD();
        else
            HideGameplayHUD();
    }

    public void RefreshGameplayHUDSessionVisibility()
    {
        SetGameplayHUDEnabled(LevelSessionManager.Instance != null && LevelSessionManager.Instance.IsSessionRunning);
    }

    public void SetGameplayHUDTopRightInfoVisible(bool visible)
    {
        if (_gameplayHUD != null)
            _gameplayHUD.SetTopRightInfoVisible(visible);
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

    public void ShowModal(UIModalBase modal)
    {
        if (modal == null || ShouldIgnoreUIRequest())
            return;

        ShowModalInternal(modal);
    }

    public void CloseModal(UIModalBase modal)
    {
        if (modal == null || ShouldIgnoreUIRequest())
            return;

        CloseModalInternal(modal);
    }

    public void CloseTopModal()
    {
        if (_activeModals.Count == 0)
            return;

        CloseModal(_activeModals[_activeModals.Count - 1]);
    }

    public void NotifyModalShown(UIModalBase modal)
    {
        if (modal == null)
            return;

        _activeModals.Remove(modal);
        _activeModals.Add(modal);
    }

    public void NotifyModalHidden(UIModalBase modal)
    {
        if (modal == null)
            return;

        _activeModals.Remove(modal);
    }

    private void ShowPanel(UIPanel panel)
    {
        if (panel == null || ShouldIgnoreUIRequest())
            return;

        ShowPanelInternal(panel);
    }

    private void HidePanel(UIPanel panel)
    {
        if (panel == null || ShouldIgnoreUIRequest())
            return;

        HidePanelInternal(panel);
    }

    private void TogglePanel(UIPanel panel)
    {
        if (panel == null || ShouldIgnoreUIRequest())
            return;

        if (panel.IsVisible)
            HidePanelInternal(panel);
        else
            ShowPanelInternal(panel);
    }

    private bool IsPanelVisible(UIPanel panel)
    {
        return panel != null && panel.IsVisible;
    }

    private void ToggleModal(UIModalBase modal)
    {
        if (modal == null || ShouldIgnoreUIRequest())
            return;

        if (modal.IsVisible)
            CloseModalInternal(modal);
        else
            ShowModalInternal(modal);
    }

    private bool IsModalVisible(UIModalBase modal)
    {
        return modal != null && modal.IsVisible;
    }

    private void RefreshGameplayHUDVisibility()
    {
        if (_gameplayHUD == null)
            return;

        bool shouldShowHUD = _shouldGameplayHUDBeVisible && !UIPanel.HasVisibleBlockingPanel;

        if (shouldShowHUD)
            ApplyGameplayHUDVisibility(force: true);
        else
            ApplyGameplayHUDVisibility(force: true);
    }

    private void ApplyGameplayHUDVisibility(bool force)
    {
        if (_gameplayHUD == null)
            return;

        bool shouldShowHUD = _shouldGameplayHUDBeVisible && !UIPanel.HasVisibleBlockingPanel;

        if (!force && ShouldIgnoreUIRequest())
            return;

        if (shouldShowHUD)
            ShowPanelInternal(_gameplayHUD);
        else
            HidePanelInternal(_gameplayHUD);
    }

    private TutorialUIOverlay ResolveTutorialOverlay()
    {
        if (_tutorialOverlay != null)
            return _tutorialOverlay;

        _tutorialOverlay = GetComponentInChildren<TutorialUIOverlay>(true);

        return _tutorialOverlay;
    }

    public TutorialUIOverlay GetTutorialOverlay()
    {
        return ResolveTutorialOverlay();
    }

    public void SetUIRequestLock(bool locked)
    {
        _uiRequestLock = locked;
    }

    public IEnumerator HideActivePanelsForSceneTransition(bool hideSplashScreen = false, bool hideLevelsScreen = false)
    {
        List<UIModalBase> activeModalsSnapshot = new List<UIModalBase>(_activeModals);
        for (int i = activeModalsSnapshot.Count - 1; i >= 0; i--)
        {
            PreparePanelForManagedTransition(activeModalsSnapshot[i]);
            yield return HidePanelRoutineInternal(activeModalsSnapshot[i]);
        }

        yield return HidePanelRoutineInternal(_pauseOverlay);
        yield return HidePanelRoutineInternal(ResolveTutorialOverlay());

        if (hideSplashScreen)
            yield return HidePanelRoutineInternal(_splashScreen);

        if (hideLevelsScreen)
            yield return HidePanelRoutineInternal(_levelsScreen);
    }

    public IEnumerator SetSplashScreenVisibilityForTransition(bool visible)
    {
        yield return SetPanelVisibilityRoutineInternal(_splashScreen, visible);
    }

    public IEnumerator SetLevelsScreenVisibilityForTransition(bool visible)
    {
        yield return SetPanelVisibilityRoutineInternal(_levelsScreen, visible);
    }

    private bool ShouldIgnoreUIRequest()
    {
        return IsUIBusy;
    }

    private void ShowModalInternal(UIModalBase modal)
    {
        if (modal.IsVisible)
        {
            NotifyModalShown(modal);
            return;
        }

        modal.Show();
        RaiseBlockingPanelShownIfNeeded(modal);
    }

    private void CloseModalInternal(UIModalBase modal)
    {
        if (!modal.IsVisible)
        {
            NotifyModalHidden(modal);
            return;
        }

        modal.Hide();
    }

    private void ShowPanelInternal(UIPanel panel)
    {
        panel.Show();
        RaiseBlockingPanelShownIfNeeded(panel);
    }

    private void HidePanelInternal(UIPanel panel)
    {
        panel.Hide();
    }

    private bool StartManagedUIFlow(IEnumerator routine)
    {
        if (routine == null || IsUIBusy)
            return false;

        _uiFlowRoutine = StartCoroutine(RunManagedUIFlow(routine));
        return true;
    }

    private IEnumerator RunManagedUIFlow(IEnumerator routine)
    {
        yield return routine;
        _uiFlowRoutine = null;
    }

    private IEnumerator ShowNoLivesModalRoutine()
    {
        yield return HidePanelRoutineInternal(_emergencyBundleModal);
        yield return ShowPanelRoutineInternal(_noLivesModal);
    }

    private IEnumerator ShowDefeatModalRoutine(int livesRemaining)
    {
        if (_defeatModal == null)
            yield break;

        PreparePanelForManagedTransition(_noLivesModal);
        yield return HidePanelRoutineInternal(_noLivesModal);
        yield return HidePanelRoutineInternal(_emergencyBundleModal);

        _defeatModal.ShowLifeLost(livesRemaining);
        yield return ShowPanelRoutineInternal(_defeatModal);
    }

    private IEnumerator ShowEmergencyBundleModalRoutine(EmergencyBundleOffer offer)
    {
        if (_emergencyBundleModal == null)
            yield break;

        PreparePanelForManagedTransition(_noLivesModal);
        yield return HidePanelRoutineInternal(_noLivesModal);

        _emergencyBundleModal.ShowWithOffer(offer);
        yield return ShowPanelRoutineInternal(_emergencyBundleModal);
    }

    private IEnumerator ShowPanelRoutineInternal(UIPanel panel)
    {
        if (panel == null)
            yield break;

        yield return panel.ShowRoutine();

        RaiseBlockingPanelShownIfNeeded(panel);
    }

    private IEnumerator HidePanelRoutineInternal(UIPanel panel)
    {
        if (panel == null)
            yield break;

        bool wasBlocking = panel.BlocksUnderlyingUIForFlow;
        string panelName = panel.name;
        yield return panel.HideRoutine();

        if (wasBlocking)
            RaiseBlockingPanelHidden(panelName);
    }

    private IEnumerator SetPanelVisibilityRoutineInternal(UIPanel panel, bool visible)
    {
        if (visible)
            yield return ShowPanelRoutineInternal(panel);
        else
            yield return HidePanelRoutineInternal(panel);
    }

    private void PreparePanelForManagedTransition(UIPanel panel)
    {
        if (panel is NoLivesModal noLivesModal)
            noLivesModal.PrepareForFlowTransitionClose();
    }

    private static void RaiseBlockingPanelShownIfNeeded(UIPanel panel)
    {
        if (panel == null || !panel.BlocksUnderlyingUIForFlow)
            return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[UIManager] Signal -> BlockingPanelShown | Source={panel.name}");
#endif
        UIEvents.RaiseBlockingPanelShown(panel.name);
    }

    private static void RaiseBlockingPanelHidden(string panelName)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[UIManager] Signal -> BlockingPanelHidden | Source={panelName}");
#endif
        UIEvents.RaiseBlockingPanelHidden(panelName);
    }

    #endregion
}
