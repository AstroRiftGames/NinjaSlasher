using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(GameStateManager))]
public sealed class GameStateBindings : MonoBehaviour
{
    private GameStateManager _manager;
    private bool _isStartupSequenceActive;

    private bool _rewardFlowWasActive;
    private bool _rewardRestorePending;
    private int _rewardRestoreFrame;
    private GameState _stateBeforeRewardFlow = GameState.Boot;

    private bool _purchaseWasProcessing;
    private bool _purchaseRestorePending;
    private int _purchaseRestoreFrame;
    private GameState _stateBeforePurchaseFlow = GameState.Boot;

    private bool _isBackgrounded;
    private bool _backgroundRestorePending;
    private int _backgroundRestoreFrame;
    private string _backgroundRestoreReason;
    private GameState _stateBeforeBackground = GameState.Boot;

    private void Awake()
    {
        _manager = GetComponent<GameStateManager>();
    }

    private void OnEnable()
    {
        SubscribeLevelFlow();
        SubscribeUIFlow();
        SyncWithActiveScene();
    }

    private void OnDisable()
    {
        UnsubscribeLevelFlow();
        UnsubscribeUIFlow();
    }

    private void Update()
    {
        UpdateRewardFlowState();
        UpdatePurchaseFlowState();
        UpdateBackgroundRestore();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            EnterBackground("ApplicationPaused");
        else
            ExitBackground("ApplicationResumed");
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            EnterBackground("ApplicationFocusLost");
        else
            ExitBackground("ApplicationFocusRegained");
    }

    private void SubscribeLevelFlow()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameEvents.OnLevelStarted += OnLevelStarted;
    }

    private void UnsubscribeLevelFlow()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameEvents.OnLevelStarted -= OnLevelStarted;
    }

    private void SubscribeUIFlow()
    {
        UIEvents.OnLevelSelectorReady += OnLevelSelectorReady;
        UIEvents.OnShowPregameModalRequested += OnShowPregameModalRequested;
        UIEvents.OnHidePregameModalRequested += OnHidePregameModalRequested;
        UIEvents.OnSceneTransitionRequested += OnSceneTransitionRequested;
        UIEvents.OnLoadLevelSelectorSceneRequested += OnLoadLevelSelectorSceneRequested;
        UIEvents.OnShowLevelSelectorRequested += OnShowLevelSelectorRequested;
        UIEvents.OnShowPauseOverlayRequested += OnShowPauseOverlayRequested;
        UIEvents.OnShowTutorialOverlayRequested += OnShowTutorialOverlayRequested;
        UIEvents.OnShowNoLivesModalRequested += OnShowNoLivesModalRequested;
        UIEvents.OnShowDefeatModalRequested += OnShowDefeatModalRequested;
        UIEvents.OnShowEmergencyBundleModalRequested += OnShowEmergencyBundleModalRequested;
        UIEvents.OnShowVictoryModalRequested += OnShowVictoryModalRequested;
        UIEvents.OnShowDailyWheelModalRequested += OnShowDailyWheelModalRequested;
        UIEvents.OnShowDailyRewardModalRequested += OnShowDailyRewardModalRequested;
        UIEvents.OnStartupSequenceStarted += OnStartupSequenceStarted;
        UIEvents.OnStartupSequenceCompleted += OnStartupSequenceCompleted;
        UIEvents.RaiseGamePaused += OnPauseStateChanged;
    }

    private void UnsubscribeUIFlow()
    {
        UIEvents.OnLevelSelectorReady -= OnLevelSelectorReady;
        UIEvents.OnShowPregameModalRequested -= OnShowPregameModalRequested;
        UIEvents.OnHidePregameModalRequested -= OnHidePregameModalRequested;
        UIEvents.OnSceneTransitionRequested -= OnSceneTransitionRequested;
        UIEvents.OnLoadLevelSelectorSceneRequested -= OnLoadLevelSelectorSceneRequested;
        UIEvents.OnShowLevelSelectorRequested -= OnShowLevelSelectorRequested;
        UIEvents.OnShowPauseOverlayRequested -= OnShowPauseOverlayRequested;
        UIEvents.OnShowTutorialOverlayRequested -= OnShowTutorialOverlayRequested;
        UIEvents.OnShowNoLivesModalRequested -= OnShowNoLivesModalRequested;
        UIEvents.OnShowDefeatModalRequested -= OnShowDefeatModalRequested;
        UIEvents.OnShowEmergencyBundleModalRequested -= OnShowEmergencyBundleModalRequested;
        UIEvents.OnShowVictoryModalRequested -= OnShowVictoryModalRequested;
        UIEvents.OnShowDailyWheelModalRequested -= OnShowDailyWheelModalRequested;
        UIEvents.OnShowDailyRewardModalRequested -= OnShowDailyRewardModalRequested;
        UIEvents.OnStartupSequenceStarted -= OnStartupSequenceStarted;
        UIEvents.OnStartupSequenceCompleted -= OnStartupSequenceCompleted;
        UIEvents.RaiseGamePaused -= OnPauseStateChanged;
    }

    private void SyncWithActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (IsLevelScene(activeScene))
            TrySetState(GameState.LevelReady, "InitialLevelSceneDetected");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsLevelScene(scene))
            TrySetState(GameState.LevelReady, "LevelSceneLoaded");
    }

    private void OnLevelStarted()
    {
        TrySetState(GameState.Playing, "LevelStarted");
    }

    private void OnLevelSelectorReady()
    {
        TrySetState(GameState.LevelSelection, "LevelSelectorReady");
    }

    private void OnShowPregameModalRequested()
    {
        TrySetState(GameState.PreGame, "ShowPregameModalRequested");
    }

    private void OnHidePregameModalRequested()
    {
        if (_manager != null && _manager.CurrentState == GameState.PreGame)
            TrySetState(GameState.LevelSelection, "HidePregameModalRequested");
    }

    private void OnSceneTransitionRequested(string _)
    {
        TrySetState(GameState.Transitioning, "SceneTransitionRequested");
    }

    private void OnLoadLevelSelectorSceneRequested()
    {
        TrySetState(GameState.Transitioning, "LoadLevelSelectorSceneRequested");
    }

    private void OnShowLevelSelectorRequested()
    {
        TrySetState(GameState.Transitioning, "ShowLevelSelectorRequested");
    }

    private void OnShowPauseOverlayRequested()
    {
        TrySetState(GameState.Paused, "PauseOverlayRequested");
    }

    private void OnShowTutorialOverlayRequested()
    {
        TrySetState(GameState.Paused, "TutorialOverlayRequested");
    }

    private void OnShowNoLivesModalRequested()
    {
        TrySetState(GameState.NoLivesWall, "NoLivesModalRequested");
    }

    private void OnShowDefeatModalRequested(int _)
    {
        TrySetState(GameState.Defeat, "DefeatModalRequested");
    }

    private void OnShowEmergencyBundleModalRequested(EmergencyBundleOffer _)
    {
        TrySetState(GameState.EmergencyBundleOffer, "EmergencyBundleModalRequested");
    }

    private void OnShowVictoryModalRequested()
    {
        TrySetState(GameState.VictoryResults, "VictoryModalRequested");
    }

    private void OnShowDailyWheelModalRequested()
    {
        if (_isStartupSequenceActive)
            TrySetState(GameState.StartupModalSequence, "DailyWheelStartupModalRequested");
    }

    private void OnShowDailyRewardModalRequested()
    {
        if (_isStartupSequenceActive)
            TrySetState(GameState.StartupModalSequence, "DailyRewardStartupModalRequested");
    }

    private void OnStartupSequenceStarted()
    {
        _isStartupSequenceActive = true;
    }

    private void OnStartupSequenceCompleted()
    {
        _isStartupSequenceActive = false;
        TrySetState(GameState.LevelSelection, "StartupSequenceCompleted");
    }

    private void OnPauseStateChanged(bool isPaused)
    {
        if (_manager == null)
            return;

        if (isPaused)
        {
            if (ShouldPromoteToPaused(_manager.CurrentState))
                TrySetState(GameState.Paused, "PauseRaised");

            return;
        }

        if (_manager.CurrentState == GameState.Paused)
            TrySetState(InferForegroundStateFromCurrentContext(), "PauseReleased");
    }

    private void UpdateRewardFlowState()
    {
        AdsManager adsManager = AdsManager.Instance;
        bool isRewardFlowActive = adsManager != null && adsManager.IsRewardedAdFlowInProgress();

        if (isRewardFlowActive && !_rewardFlowWasActive)
        {
            _rewardFlowWasActive = true;
            _rewardRestorePending = false;
            _stateBeforeRewardFlow = _manager != null ? _manager.CurrentState : GameState.Boot;
            TrySetState(GameState.RewardFlow, "RewardedAdFlowStarted");
            return;
        }

        if (!isRewardFlowActive && _rewardFlowWasActive)
        {
            _rewardFlowWasActive = false;
            _rewardRestorePending = true;
            _rewardRestoreFrame = Time.frameCount;
        }

        if (_rewardRestorePending && Time.frameCount > _rewardRestoreFrame)
        {
            _rewardRestorePending = false;

            if (_manager != null && _manager.CurrentState == GameState.RewardFlow)
                TrySetState(_stateBeforeRewardFlow, "RewardedAdFlowEnded");
        }
    }

    private void UpdatePurchaseFlowState()
    {
        IAPManager iapManager = IAPManager.Instance;
        bool isProcessing = iapManager != null && iapManager.PurchaseState == PurchaseState.Processing;

        if (isProcessing && !_purchaseWasProcessing)
        {
            _purchaseWasProcessing = true;
            _purchaseRestorePending = false;
            _stateBeforePurchaseFlow = _manager != null ? _manager.CurrentState : GameState.Boot;
            TrySetState(GameState.PurchaseProcessing, "PurchaseProcessingStarted");
            return;
        }

        if (!isProcessing && _purchaseWasProcessing)
        {
            _purchaseWasProcessing = false;
            _purchaseRestorePending = true;
            _purchaseRestoreFrame = Time.frameCount;
        }

        if (_purchaseRestorePending && Time.frameCount > _purchaseRestoreFrame)
        {
            _purchaseRestorePending = false;

            if (_manager != null && _manager.CurrentState == GameState.PurchaseProcessing)
                TrySetState(_stateBeforePurchaseFlow, "PurchaseProcessingEnded");
        }
    }

    private void EnterBackground(string reason)
    {
        if (_isBackgrounded)
            return;

        _isBackgrounded = true;
        _backgroundRestorePending = false;
        _stateBeforeBackground = _manager != null ? _manager.CurrentState : GameState.Boot;
        TrySetState(GameState.AppBackgroundPaused, reason);
    }

    private void ExitBackground(string reason)
    {
        if (!_isBackgrounded)
            return;

        _isBackgrounded = false;
        _backgroundRestorePending = true;
        _backgroundRestoreReason = reason;
        _backgroundRestoreFrame = Time.frameCount;
    }

    private void UpdateBackgroundRestore()
    {
        if (!_backgroundRestorePending || Time.frameCount <= _backgroundRestoreFrame)
            return;

        _backgroundRestorePending = false;

        if (_manager != null && _manager.CurrentState == GameState.AppBackgroundPaused)
            TrySetState(InferBackgroundRestoreState(), _backgroundRestoreReason);
    }

    private GameState InferBackgroundRestoreState()
    {
        if (_stateBeforeBackground != GameState.AppBackgroundPaused)
            return _stateBeforeBackground;

        return InferForegroundStateFromCurrentContext();
    }

    private bool TrySetState(GameState newState, string reason)
    {
        return _manager != null && _manager.TrySetState(newState, reason, this);
    }

    private static bool ShouldPromoteToPaused(GameState currentState)
    {
        return currentState == GameState.Playing ||
               currentState == GameState.LevelReady;
    }

    private static bool IsLevelScene(string sceneName)
    {
        return !string.IsNullOrEmpty(sceneName) &&
               (sceneName.StartsWith("Level_") || sceneName.Contains("Level"));
    }

    private static bool IsLevelScene(Scene scene)
    {
        return scene.IsValid() && IsLevelScene(scene.name);
    }

    private static GameState InferForegroundStateFromCurrentContext()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (IsLevelScene(activeScene))
        {
            if (LevelSessionManager.Instance != null)
            {
                if (LevelSessionManager.Instance.IsSessionRunning)
                    return GameState.Playing;

                if (LevelSessionManager.Instance.HasActiveSession || LevelSessionManager.Instance.IsLevelActive)
                    return GameState.LevelReady;
            }

            return GameState.LevelReady;
        }

        return GameState.LevelSelection;
    }
}
