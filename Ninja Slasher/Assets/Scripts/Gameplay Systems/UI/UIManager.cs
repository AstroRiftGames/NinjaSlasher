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
    }

    private void InitializeManagers()
    {
        _buttonManager = GetComponent<ButtonManager>();
        _gameplayUIManager = GetComponent<GameplayUIManager>();
        _preGameUIManager = GetComponent<PreGameUIManager>();
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
        GameEvents.OnLevelResultReady -= OnLevelResultReady;
    }

    #endregion

    private void OnLevelResultReady(LevelResult result)
    {
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

    #endregion

    #region MODALS

    private void ShowPregameModal() => ShowModal(_pregameModal);
    private void HidePregameModal() => CloseModal(_pregameModal);
    private void TogglePregameModal() => ToggleModal(_pregameModal);

    private void ShowNoLivesModal()
    {
        CloseModal(_emergencyBundleModal);
        ShowModal(_noLivesModal);
    }

    private void HideNoLivesModal() => CloseModal(_noLivesModal);

    private void ShowDefeatModal(int livesRemaining)
    {
        _noLivesModal?.HideForFlowTransition();
        CloseModal(_emergencyBundleModal);

        if (_defeatModal != null)
        {
            _defeatModal.ShowLifeLost(livesRemaining);
            ShowModal(_defeatModal);
        }
    }

    private void HideDefeatModal() => CloseModal(_defeatModal);

    private void ShowEmergencyBundleModal(EmergencyBundleOffer offer)
    {
        Debug.Log($"[UIManager] ShowEmergencyBundleModal | modal assigned={_emergencyBundleModal != null}");
        _noLivesModal?.HideForFlowTransition();

        if (_emergencyBundleModal != null)
        {
            _emergencyBundleModal.ShowWithOffer(offer);
            ShowModal(_emergencyBundleModal);
        }
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
        HidePanel(_gameplayHUD);
    }

    public void SetGameplayHUDEnabled(bool enabled)
    {
        if (enabled)
            ShowGameplayHUD();
        else
            HideGameplayHUD();
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
        if (modal == null)
            return;

        if (modal.IsVisible)
        {
            NotifyModalShown(modal);
            return;
        }

        modal.Show();
    }

    public void CloseModal(UIModalBase modal)
    {
        if (modal == null)
            return;

        if (!modal.IsVisible)
        {
            NotifyModalHidden(modal);
            return;
        }

        modal.Hide();
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

    private void ToggleModal(UIModalBase modal)
    {
        if (modal == null)
            return;

        if (modal.IsVisible)
            CloseModal(modal);
        else
            ShowModal(modal);
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
            ShowPanel(_gameplayHUD);
        else
            HidePanel(_gameplayHUD);
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

    public bool HasBlockingPanelForLevelSelection()
    {
        return IsPanelVisible(_pauseOverlay)
            || IsPanelVisible(ResolveTutorialOverlay())
            || IsPanelVisible(_splashScreen)
            || IsModalVisible(_pregameModal)
            || IsModalVisible(_noLivesModal)
            || IsModalVisible(_defeatModal)
            || IsModalVisible(_emergencyBundleModal)
            || IsModalVisible(_creditsModal)
            || IsModalVisible(_profileModal)
            || IsModalVisible(_dailyRewardModal)
            || IsModalVisible(_dailyWheelModal)
            || IsModalVisible(_storeModal)
            || IsModalVisible(_victoryModal);
    }

    #endregion
}
