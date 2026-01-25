using System;
using UnityEngine;

public static class UIEvents
{
    #region PANEL NAVIGATION EVENTS

    public static event Action<string> OnPanelOpenRequested;
    public static event Action<string> OnPanelCloseRequested;
    public static event Action OnAllPanelsCloseRequested;

    public static void RequestOpenPanel(string panelName)
    {
        OnPanelOpenRequested?.Invoke(panelName);
    }

    public static void RequestClosePanel(string panelName)
    {
        OnPanelCloseRequested?.Invoke(panelName);
    }

    public static void RequestCloseAllPanels()
    {
        OnAllPanelsCloseRequested?.Invoke();
    }

    #endregion

    #region SCENE TRANSITION EVENTS

    public static event Action<string> OnSceneTransitionRequested;
    public static event Action OnShowLevelSelectorRequested;
    public static event Action OnRestartLevelRequested;

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

    #endregion

    #region RESULTS AND REWARDS EVENTS

    public static event Action OnShowResultsPanelRequested;
    public static event Action OnShowNoLivesPanelRequested;
    public static event Action OnShowLifeLostPanelRequested;
    public static event Action OnShowDailyRewardRequested;

    public static void RequestShowResultsPanel()
    {
        OnShowResultsPanelRequested?.Invoke();
    }

    public static void RequestShowNoLivesPanel()
    {
        OnShowNoLivesPanelRequested?.Invoke();
    }

    public static void RequestShowLifeLostPanel()
    {
        OnShowLifeLostPanelRequested?.Invoke();
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
        OnAllPanelsCloseRequested = null;
    }

    public static void ClearSceneTransitionEvents()
    {
        OnSceneTransitionRequested = null;
        OnShowLevelSelectorRequested = null;
        OnRestartLevelRequested = null;
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
    }

    public static void ClearPanelDisplayEvents()
    {
        OnShowResultsPanelRequested = null;
        OnShowNoLivesPanelRequested = null;
        OnShowLifeLostPanelRequested = null;
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
        ClearSceneTransitionEvents();
        ClearButtonEvents();
        ClearUpdateEvents();
        ClearPanelDisplayEvents();
        ClearProfileEvents();
    }

    #endregion
}