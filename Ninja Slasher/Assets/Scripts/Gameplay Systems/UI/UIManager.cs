using System.Collections;
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
    [SerializeField] private NoLivesOverlay _noLivesOverlay;
    [SerializeField] private DefeatOverlay _defeatOverlay;
    [SerializeField] private EmergencyBundleOverlay _emergencyBundleOverlay;

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
    [SerializeField] private VictoryModal _victoryModal;

    [Header("HUD")]
    [SerializeField] private GameplayHUD _gameplayHUD;

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

        UIEvents.OnShowDefeatOverlayRequested += ShowDefeatOverlay;

        UIEvents.OnShowEmergencyBundleOverlayRequested += ShowEmergencyBundleOverlay;
        UIEvents.OnHideEmergencyBundleOverlayRequested += HideEmergencyBundleOverlay;

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

    private void UnsubscribeFromUIEvents()
    {
        UIEvents.OnShowPauseOverlayRequested -= ShowPauseOverlay;
        UIEvents.OnHidePauseOverlayRequested -= HidePauseOverlay;
        UIEvents.OnTogglePauseOverlayRequested -= TogglePauseOverlay;

        UIEvents.OnShowNoLivesOverlayRequested -= ShowNoLivesOverlay;
        UIEvents.OnHideNoLivesOverlayRequested -= HideNoLivesOverlay;

        UIEvents.OnShowDefeatOverlayRequested -= ShowDefeatOverlay;

        UIEvents.OnShowEmergencyBundleOverlayRequested -= ShowEmergencyBundleOverlay;
        UIEvents.OnHideEmergencyBundleOverlayRequested -= HideEmergencyBundleOverlay;

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

    #endregion

    #region OVERLAYS

    private void ShowPauseOverlay() => ShowPanel(_pauseOverlay);
    private void HidePauseOverlay() => HidePanel(_pauseOverlay);
    public void TogglePauseOverlay() => TogglePanel(_pauseOverlay);

    private void ShowNoLivesOverlay() => ShowPanel(_noLivesOverlay);
    private void HideNoLivesOverlay() => HidePanel(_noLivesOverlay);

    private void ShowDefeatOverlay(int livesRemaining)
    {
        if (_defeatOverlay != null)
            _defeatOverlay.ShowLifeLost(livesRemaining);
    }

    public void HideDefeatOverlay()
    {
        if (_defeatOverlay != null)
            _defeatOverlay.Hide();
    }

    private void ShowEmergencyBundleOverlay(EmergencyBundleOffer offer)
    {
        Debug.Log($"[UIManager] ShowEmergencyBundleOverlay | overlay assigned={_emergencyBundleOverlay != null}");
        if (_emergencyBundleOverlay != null)
            _emergencyBundleOverlay.ShowWithOffer(offer);
    }

    private void HideEmergencyBundleOverlay()
    {
        HidePanel(_emergencyBundleOverlay);
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
            _levelsScreen.ResetAnimationStateForScreenReturn();
    }

    #endregion

    #region MODALS

    private void ShowCreditsModal() => ShowPanel(_creditsModal);
    private void HideCreditsModal() => HidePanel(_creditsModal);
    private void ToggleCreditsModal() => TogglePanel(_creditsModal);

    private void ShowProfileModal() => ShowPanel(_profileModal);
    private void HideProfileModal() => HidePanel(_profileModal);
    private void ToggleProfileModal() => TogglePanel(_profileModal);

    private void ShowDailyRewardModal()
    {
        ShowPanel(_dailyRewardModal);
        DailyRewardUIManager.Instance?.ShowDailyReward();
    }
    private void HideDailyRewardModal() => HidePanel(_dailyRewardModal);
    private void ToggleDailyRewardModal() => TogglePanel(_dailyRewardModal);
    public bool IsDailyRewardModalVisible() => IsPanelVisible(_dailyRewardModal);

    private void OnHideDailyRewardRequested()
    {
        HideDailyRewardModal();
    }

    private void ShowDailyWheelModal() => ShowPanel(_dailyWheelModal);
    private void HideDailyWheelModal() => HidePanel(_dailyWheelModal);
    private void ToggleDailyWheelModal() => TogglePanel(_dailyWheelModal);
    public bool IsDailyWheelModalVisible() => IsPanelVisible(_dailyWheelModal);
    private void OnHideDailyWheelRequested()
    {
        Debug.Log("[DailySequence] Wheel hide requested via UIEvents");
        HideDailyWheelModal();
    }

    private void ShowStoreModal() => ShowPanel(_storeModal);
    private void HideStoreModal() => HidePanel(_storeModal);
    private void ToggleStoreModal() => TogglePanel(_storeModal);

    private void ShowVictoryModal() => ShowPanel(_victoryModal);
    private void HideVictoryModal() => HidePanel(_victoryModal);
    private void ToggleVictoryModal() => TogglePanel(_victoryModal);

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
}
