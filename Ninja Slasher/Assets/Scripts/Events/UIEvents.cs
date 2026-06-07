using System;
using UnityEngine;

public static class UIEvents
{
    #region PANEL NAVIGATION EVENTS

    public static event Action<string> OnPanelOpenRequested;
    public static event Action<string> OnPanelCloseRequested;
    public static event Action<string> OnPanelToggleRequested;
    public static event Action OnAllPanelsCloseRequested;
    public static event Action<string> OnBlockingPanelShown;
    public static event Action<string> OnBlockingPanelHidden;

    public static void RequestOpenPanel(string panelName)
    {
        OnPanelOpenRequested?.Invoke(panelName);
    }

    public static void RequestClosePanel(string panelName)
    {
        OnPanelCloseRequested?.Invoke(panelName);
    }

    public static void RequestTogglePanel(string panelName)
    {
        OnPanelToggleRequested?.Invoke(panelName);
    }

    public static void RequestCloseAllPanels()
    {
        OnAllPanelsCloseRequested?.Invoke();
    }

    public static void RaiseBlockingPanelShown(string source)
    {
        OnBlockingPanelShown?.Invoke(source);
    }

    public static void RaiseBlockingPanelHidden(string source)
    {
        OnBlockingPanelHidden?.Invoke(source);
    }

    #endregion

    #region OVERLAY EVENTS

    public static event Action OnShowPauseOverlayRequested;
    public static event Action OnHidePauseOverlayRequested;
    public static event Action OnTogglePauseOverlayRequested;
    public static event Action OnShowTutorialOverlayRequested;
    public static event Action OnHideTutorialOverlayRequested;

    public static void RequestShowTutorialOverlay()
    {
        OnShowTutorialOverlayRequested?.Invoke();
    }

    public static void RequestHideTutorialOverlay()
    {
        OnHideTutorialOverlayRequested?.Invoke();
    }

    public static bool HasTutorialOverlayListener()
    {
        return OnShowTutorialOverlayRequested != null && OnHideTutorialOverlayRequested != null;
    }

    public static void RequestShowPauseOverlay()
    {
        OnShowPauseOverlayRequested?.Invoke();
    }

    public static void RequestHidePauseOverlay()
    {
        OnHidePauseOverlayRequested?.Invoke();
    }

    public static void RequestTogglePauseOverlay()
    {
        OnTogglePauseOverlayRequested?.Invoke();
    }

    #endregion

    #region SCREEN EVENTS

    public static event Action OnShowSplashScreenRequested;
    public static event Action OnHideSplashScreenRequested;

    public static event Action OnShowLevelsScreenRequested;
    public static event Action OnHideLevelsScreenRequested;

    public static void RequestShowSplashScreen()
    {
        OnShowSplashScreenRequested?.Invoke();
    }

    public static void RequestHideSplashScreen()
    {
        OnHideSplashScreenRequested?.Invoke();
    }

    public static void RequestShowLevelsScreen()
    {
        OnShowLevelsScreenRequested?.Invoke();
    }

    public static void RequestHideLevelsScreen()
    {
        OnHideLevelsScreenRequested?.Invoke();
    }

    #endregion

    #region MODAL EVENTS

    public static event Action OnShowPregameModalRequested;
    public static event Action OnHidePregameModalRequested;
    public static event Action OnTogglePregameModalRequested;

    public static event Action OnShowNoLivesModalRequested;
    public static event Action OnHideNoLivesModalRequested;

    public static event Action<int> OnShowDefeatModalRequested;
    public static event Action OnHideDefeatModalRequested;

    public static event Action<EmergencyBundleOffer> OnShowEmergencyBundleModalRequested;
    public static event Action OnHideEmergencyBundleModalRequested;

    public static event Action OnShowCreditsModalRequested;
    public static event Action OnHideCreditsModalRequested;
    public static event Action OnToggleCreditsModalRequested;

    public static event Action OnShowProfileModalRequested;
    public static event Action OnHideProfileModalRequested;
    public static event Action OnToggleProfileModalRequested;

    public static event Action OnShowDailyRewardModalRequested;
    public static event Action OnHideDailyRewardModalRequested;
    public static event Action OnToggleDailyRewardModalRequested;

    public static event Action OnShowDailyWheelModalRequested;
    public static event Action OnHideDailyWheelModalRequested;
    public static event Action OnToggleDailyWheelModalRequested;

    public static event Action OnWheelSequenceCompleted;
    public static event Action OnDailyWheelModalClosed;
    public static event Action OnDailyRewardModalClosed;
    public static event Action OnStartupSequenceStarted;
    public static event Action OnStartupSequenceCompleted;

    public static event Action OnShowStoreModalRequested;
    public static event Action OnHideStoreModalRequested;
    public static event Action OnToggleStoreModalRequested;

    public static event Action OnShowVictoryModalRequested;
    public static event Action OnHideVictoryModalRequested;
    public static event Action OnToggleVictoryModalRequested;
    public static VictoryContext CurrentVictoryContext { get; private set; }

    public static void RequestShowPregameModal()
    {
        OnShowPregameModalRequested?.Invoke();
    }

    public static void RequestHidePregameModal()
    {
        OnHidePregameModalRequested?.Invoke();
    }

    public static void RequestTogglePregameModal()
    {
        OnTogglePregameModalRequested?.Invoke();
    }

    public static void RequestShowNoLivesModal()
    {
        OnShowNoLivesModalRequested?.Invoke();
    }

    public static void RequestHideNoLivesModal()
    {
        OnHideNoLivesModalRequested?.Invoke();
    }

    public static void RequestShowDefeatModal(int livesRemaining)
    {
        OnShowDefeatModalRequested?.Invoke(livesRemaining);
    }

    public static void RequestHideDefeatModal()
    {
        OnHideDefeatModalRequested?.Invoke();
    }

    public static void RequestShowEmergencyBundleModal(EmergencyBundleOffer offer)
    {
        OnShowEmergencyBundleModalRequested?.Invoke(offer);
    }

    public static void RequestHideEmergencyBundleModal()
    {
        OnHideEmergencyBundleModalRequested?.Invoke();
    }

    public static bool HasEmergencyBundleModalListener()
    {
        return OnShowEmergencyBundleModalRequested != null;
    }

    public static void RequestShowCreditsModal()
    {
        OnShowCreditsModalRequested?.Invoke();
    }

    public static void RequestHideCreditsModal()
    {
        OnHideCreditsModalRequested?.Invoke();
    }

    public static void RequestToggleCreditsModal()
    {
        OnToggleCreditsModalRequested?.Invoke();
    }

    public static void RequestShowProfileModal()
    {
        OnShowProfileModalRequested?.Invoke();
    }

    public static void RequestHideProfileModal()
    {
        OnHideProfileModalRequested?.Invoke();
    }

    public static void RequestToggleProfileModal()
    {
        OnToggleProfileModalRequested?.Invoke();
    }

    public static void RequestShowDailyRewardModal()
    {
        OnShowDailyRewardModalRequested?.Invoke();
    }

    public static void RequestHideDailyRewardModal()
    {
        OnHideDailyRewardModalRequested?.Invoke();
    }

    public static void RequestToggleDailyRewardModal()
    {
        OnToggleDailyRewardModalRequested?.Invoke();
    }

    public static void RequestShowDailyWheelModal()
    {
        OnShowDailyWheelModalRequested?.Invoke();
    }

    public static void RequestHideDailyWheelModal()
    {
        OnHideDailyWheelModalRequested?.Invoke();
    }

    public static void RequestToggleDailyWheelModal()
    {
        OnToggleDailyWheelModalRequested?.Invoke();
    }

    public static void RaiseWheelSequenceCompleted()
    {
        OnWheelSequenceCompleted?.Invoke();
    }

    public static void RaiseDailyWheelModalClosed()
    {
        OnDailyWheelModalClosed?.Invoke();
    }

    public static void RaiseDailyRewardModalClosed()
    {
        OnDailyRewardModalClosed?.Invoke();
    }

    public static void RaiseStartupSequenceStarted()
    {
        OnStartupSequenceStarted?.Invoke();
    }

    public static void RaiseStartupSequenceCompleted()
    {
        OnStartupSequenceCompleted?.Invoke();
    }

    public static void RequestShowStoreModal()
    {
        OnShowStoreModalRequested?.Invoke();
    }

    public static void RequestHideStoreModal()
    {
        OnHideStoreModalRequested?.Invoke();
    }

    public static void RequestToggleStoreModal()
    {
        OnToggleStoreModalRequested?.Invoke();
    }

    public static void RequestShowVictoryModal()
    {
        OnShowVictoryModalRequested?.Invoke();
    }

    public static void RequestShowVictoryModal(VictoryContext context)
    {
        SetVictoryContext(context);
        OnShowVictoryModalRequested?.Invoke();
    }

    public static void SetVictoryContext(VictoryContext context)
    {
        CurrentVictoryContext = context;
    }

    public static void RequestHideVictoryModal()
    {
        OnHideVictoryModalRequested?.Invoke();
    }

    public static void RequestToggleVictoryModal()
    {
        OnToggleVictoryModalRequested?.Invoke();
    }

    #endregion

    #region HUD EVENTS

    public static event Action OnShowGameplayHUDRequested;
    public static event Action OnHideGameplayHUDRequested;

    public static void RequestShowGameplayHUD()
    {
        OnShowGameplayHUDRequested?.Invoke();
    }

    public static void RequestHideGameplayHUD()
    {
        OnHideGameplayHUDRequested?.Invoke();
    }

    #endregion

    #region SCENE TRANSITION EVENTS

    public static event Action<string> OnSceneTransitionRequested;
    public static event Action OnShowLevelSelectorRequested;
    public static event Action OnRestartLevelRequested;
    public static event Action OnLoadLevelSelectorSceneRequested;

    public static event Action OnLevelSelectorReady;
    public static event Action OnTransitionFinished;

    public static void RequestSceneTransition(string sceneName)
    {
        OnSceneTransitionRequested?.Invoke(sceneName);
    }

    public static void RequestShowLevelSelector()
    {
        OnShowLevelSelectorRequested?.Invoke();
    }

    public static void RequestRestartLevel()
    {
        OnRestartLevelRequested?.Invoke();
    }

    public static void RequestLoadLevelSelectorScene()
    {
        OnLoadLevelSelectorSceneRequested?.Invoke();
    }

    public static void RaiseLevelSelectorReady()
    {
        OnLevelSelectorReady?.Invoke();
    }

    public static void RaiseTransitionFinished()
    {
        OnTransitionFinished?.Invoke();
    }

    #endregion

    #region LEVEL PREVIEW EVENTS

    public static event Action<string> OnLevelPreviewRequested; // string = sceneName
    public static event Action OnLevelPreviewCancelled;
    public static event Action OnLevelPreviewConfirmed;

    public static void RequestLevelPreview(string sceneName)
    {
        OnLevelPreviewRequested?.Invoke(sceneName);
    }

    public static void CancelLevelPreview()
    {
        OnLevelPreviewCancelled?.Invoke();
    }

    public static void ConfirmLevelPreview()
    {
        OnLevelPreviewConfirmed?.Invoke();
    }

    #endregion

    #region LEVEL LOCKED EVENTS

    public static event Action<int> OnLevelLockedMessageRequested; // int = levelId

    public static void RequestShowLevelLockedMessage(int levelId)
    {
        OnLevelLockedMessageRequested?.Invoke(levelId);
    }

    #endregion

    #region SPECIFIC BUTTON EVENTS

    public static event Action OnPauseButtonPressed;
    public static event Action OnResumeButtonPressed;
    public static event Action OnQuitToMenuPressed;
    public static event Action OnRetryButtonPressed;

    public static void RaisePausePressed()
    {
        OnPauseButtonPressed?.Invoke();
    }

    public static void RaiseResumePressed()
    {
        OnResumeButtonPressed?.Invoke();
    }

    public static void RaiseQuitToMenuPressed()
    {
        OnQuitToMenuPressed?.Invoke();
    }

    public static void RaiseRetryPressed()
    {
        OnRetryButtonPressed?.Invoke();
    }

    #endregion

    #region UI UPDATE EVENTS

    public static event Action<int> OnUILivesUpdateRequested;
    public static event Action<float> OnUITimerUpdateRequested;
    public static event Action<string> OnUIPowerUpTextUpdateRequested;
    public static event Action<bool> RaiseGamePaused;

    public static void RequestUpdateLivesUI(int lives)
    {
        OnUILivesUpdateRequested?.Invoke(lives);
    }

    public static void RequestUpdateTimerUI(float time)
    {
        OnUITimerUpdateRequested?.Invoke(time);
    }

    public static void RequestUpdatePowerUpUI(string text)
    {
        OnUIPowerUpTextUpdateRequested?.Invoke(text);
    }

    public static void RaisePause(bool isPaused)
    {
        RaiseGamePaused?.Invoke(isPaused);
    }

    #endregion

    #region RESULTS AND REWARDS EVENTS

    public static event Action OnShowVictoryPanelRequested;
    public static event Action OnShowNoLivesPanelRequested;
    public static event Action OnShowDefeatPanelRequested;
    public static event Action OnShowDailyRewardRequested;

    public static void RequestShowVictoryPanel()
    {
        OnShowVictoryPanelRequested?.Invoke();
    }

    public static void RequestShowNoLivesPanel()
    {
        OnShowNoLivesPanelRequested?.Invoke();
    }

    public static void RequestShowLifeLostPanel()
    {
        OnShowDefeatPanelRequested?.Invoke();
    }

    public static void RequestShowDailyReward()
    {
        OnShowDailyRewardRequested?.Invoke();
    }

    #endregion

    #region PROFILE AND USER DATA EVENTS

    public static event Action<string> OnNicknameChanged;
    public static event Action<int> OnUserIconChanged;

    public static void RaiseNicknameChanged(string newNickname)
    {
        OnNicknameChanged?.Invoke(newNickname);
    }

    public static void RaiseUserIconChanged(int iconIndex)
    {
        OnUserIconChanged?.Invoke(iconIndex);
    }

    #endregion

    #region CLEANING SUPPLIES

    public static void ClearNavigationEvents()
    {
        OnPanelOpenRequested = null;
        OnPanelCloseRequested = null;
        OnPanelToggleRequested = null;
        OnAllPanelsCloseRequested = null;
        OnBlockingPanelShown = null;
        OnBlockingPanelHidden = null;
    }

    public static void ClearOverlayEvents()
    {
        OnShowPauseOverlayRequested = null;
        OnHidePauseOverlayRequested = null;
        OnTogglePauseOverlayRequested = null;
        OnShowTutorialOverlayRequested = null;
        OnHideTutorialOverlayRequested = null;
    }

    public static void ClearScreenEvents()
    {
        OnShowSplashScreenRequested = null;
        OnHideSplashScreenRequested = null;
        OnShowLevelsScreenRequested = null;
        OnHideLevelsScreenRequested = null;
    }

    public static void ClearModalEvents()
    {
        OnShowPregameModalRequested = null;
        OnHidePregameModalRequested = null;
        OnTogglePregameModalRequested = null;
        OnShowNoLivesModalRequested = null;
        OnHideNoLivesModalRequested = null;
        OnShowDefeatModalRequested = null;
        OnHideDefeatModalRequested = null;
        OnShowEmergencyBundleModalRequested = null;
        OnHideEmergencyBundleModalRequested = null;
        OnShowCreditsModalRequested = null;
        OnHideCreditsModalRequested = null;
        OnToggleCreditsModalRequested = null;
        OnShowProfileModalRequested = null;
        OnHideProfileModalRequested = null;
        OnToggleProfileModalRequested = null;
        OnShowDailyRewardModalRequested = null;
        OnHideDailyRewardModalRequested = null;
        OnToggleDailyRewardModalRequested = null;
        OnShowDailyWheelModalRequested = null;
        OnHideDailyWheelModalRequested = null;
        OnToggleDailyWheelModalRequested = null;
        OnWheelSequenceCompleted = null;
        OnDailyWheelModalClosed = null;
        OnDailyRewardModalClosed = null;
        OnStartupSequenceStarted = null;
        OnStartupSequenceCompleted = null;
        OnShowStoreModalRequested = null;
        OnHideStoreModalRequested = null;
        OnToggleStoreModalRequested = null;
        OnShowVictoryModalRequested = null;
        OnHideVictoryModalRequested = null;
        OnToggleVictoryModalRequested = null;
        CurrentVictoryContext = null;
    }

    public static void ClearHUDEvents()
    {
        OnShowGameplayHUDRequested = null;
        OnHideGameplayHUDRequested = null;
    }

    public static void ClearLevelPreviewEvents()
    {
        OnLevelPreviewRequested = null;
        OnLevelPreviewCancelled = null;
        OnLevelPreviewConfirmed = null;
        OnLevelLockedMessageRequested = null;
    }

    public static void ClearSceneTransitionEvents()
    {
        OnSceneTransitionRequested = null;
        OnShowLevelSelectorRequested = null;
        OnRestartLevelRequested = null;
        OnLoadLevelSelectorSceneRequested = null;
        OnLevelSelectorReady = null;
        OnTransitionFinished = null;
    }

    public static void ClearButtonEvents()
    {
        OnPauseButtonPressed = null;
        OnResumeButtonPressed = null;
        OnQuitToMenuPressed = null;
        OnRetryButtonPressed = null;
    }

    public static void ClearUpdateEvents()
    {
        OnUILivesUpdateRequested = null;
        OnUITimerUpdateRequested = null;
        OnUIPowerUpTextUpdateRequested = null;
        RaiseGamePaused = null;
    }

    public static void ClearPanelDisplayEvents()
    {
        OnShowVictoryPanelRequested = null;
        OnShowNoLivesPanelRequested = null;
        OnShowDefeatPanelRequested = null;
        OnShowDailyRewardRequested = null;
    }

    public static void ClearProfileEvents()
    {
        OnNicknameChanged = null;
        OnUserIconChanged = null;
    }

    public static void ClearAllUIEvents()
    {
        ClearNavigationEvents();
        ClearOverlayEvents();
        ClearScreenEvents();
        ClearModalEvents();
        ClearHUDEvents();
        ClearLevelPreviewEvents();
        ClearSceneTransitionEvents();
        ClearButtonEvents();
        ClearUpdateEvents();
        ClearPanelDisplayEvents();
        ClearProfileEvents();
    }

    #endregion
}
