using System;
using UnityEngine;

public sealed class DailyStartupSequence : IDisposable
{
    private bool _isLevelSelectorReady;
    private bool _isWaitingForWheel;
    private bool _isWaitingForWheelClose;
    private bool _isWaitingForReward;
    private bool _isRunning;
    private bool _isRewardSystemReady;
    private bool _shouldRunOnNextLevelSelectorReady;
    private bool _isWheelSystemReady;
    
    public bool ShouldRunOnNextLevelSelectorReady => _shouldRunOnNextLevelSelectorReady;

    public DailyStartupSequence(MonoBehaviour runner)
    {
        DailyRewardSystem.OnBootstrapped += OnRewardSystemBootstrapped;
        DailyWheelSystem.OnBootstrapped += OnWheelSystemBootstrapped;
        UIEvents.OnLevelSelectorReady += OnLevelSelectorReady;
        UIEvents.OnWheelSequenceCompleted += OnWheelSequenceCompleted;
        UIEvents.OnDailyWheelModalClosed += OnDailyWheelModalClosed;
        UIEvents.OnDailyRewardModalClosed += OnDailyRewardModalClosed;
        SyncSystemReadiness("Constructor");
    }

    public void Dispose()
    {
        DailyRewardSystem.OnBootstrapped -= OnRewardSystemBootstrapped;
        DailyWheelSystem.OnBootstrapped -= OnWheelSystemBootstrapped;
        UIEvents.OnLevelSelectorReady -= OnLevelSelectorReady;
        UIEvents.OnWheelSequenceCompleted -= OnWheelSequenceCompleted;
        UIEvents.OnDailyWheelModalClosed -= OnDailyWheelModalClosed;
        UIEvents.OnDailyRewardModalClosed -= OnDailyRewardModalClosed;
    }

    public void ConfigureNextLevelSelectorEntry(bool shouldRunStartupSequence)
    {
        _shouldRunOnNextLevelSelectorReady = shouldRunStartupSequence;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[DailyStartupSequence] ConfigureNextLevelSelectorEntry -> shouldRun={shouldRunStartupSequence}");
#endif
    }

    private void OnRewardSystemBootstrapped()
    {
        _isRewardSystemReady = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[DailyStartupSequence] Signal -> DailyRewardSystem.OnBootstrapped");
#endif
        TryAdvanceSequence();
    }

    private void OnWheelSystemBootstrapped()
    {
        _isWheelSystemReady = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[DailyStartupSequence] Signal -> DailyWheelSystem.OnBootstrapped");
#endif
        TryAdvanceSequence();
    }

    private void OnLevelSelectorReady()
    {
        if (_isRunning)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[DailyStartupSequence] Signal -> LevelSelectorReady ignored because startup sequence is already running.");
#endif
            return;
        }

        if (!_shouldRunOnNextLevelSelectorReady)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[DailyStartupSequence] Signal -> LevelSelectorReady ignored because this entry does not own startup sequence.");
#endif
            return;
        }

        GameData data = SaveManager.Instance != null ? SaveManager.Instance.GetGameData() : null;
        if (FirstTimeWelcomeSaveState.IsWelcomePendingForCurrentEntry(data))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[DailyStartupSequence] Signal -> LevelSelectorReady deferred because first-time welcome is pending.");
#endif
            UIEvents.RaiseStartupSequenceCompleted();
            return;
        }

        _shouldRunOnNextLevelSelectorReady = false;

        if (FirstTimeWelcomeSaveState.WasWelcomeCompletedToday(data))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[DailyStartupSequence] Signal -> LevelSelectorReady suppressed because first-time welcome was completed today.");
#endif
            UIEvents.RaiseStartupSequenceCompleted();
            return;
        }

        SyncSystemReadiness("OnLevelSelectorReady");
        _isLevelSelectorReady = true;
        _isRunning = true;
        _isWaitingForWheel = false;
        _isWaitingForWheelClose = false;
        _isWaitingForReward = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[DailyStartupSequence] Signal -> StartupSequenceStarted");
#endif
        UIEvents.RaiseStartupSequenceStarted();

        TryAdvanceSequence();
    }

    private void OnWheelSequenceCompleted()
    {
        if (!_isWaitingForWheel) return;

        _isWaitingForWheel = false;
        _isWaitingForWheelClose = true;
        UIEvents.RequestHideDailyWheelModal();
    }

    private void OnDailyWheelModalClosed()
    {
        if (!_isWaitingForWheel && !_isWaitingForWheelClose) return;

        _isWaitingForWheel = false;
        _isWaitingForWheelClose = false;
        TryAdvanceSequence();
    }

    private void OnDailyRewardModalClosed()
    {
        if (!_isWaitingForReward) return;

        _isWaitingForReward = false;
        CompleteSequence();
    }

    private void TryAdvanceSequence()
    {
        if (!_isRunning || !_isLevelSelectorReady) return;
        if (_isWaitingForWheel || _isWaitingForWheelClose || _isWaitingForReward) return;

        SyncSystemReadiness("TryAdvanceSequence");

        if (DailyWheelSystem.Instance == null || DailyRewardSystem.Instance == null)
        {
            CompleteSequence();
            return;
        }

        if (!_isWheelSystemReady || !_isRewardSystemReady)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[DailyStartupSequence] Waiting -> rewardReady={_isRewardSystemReady} | wheelReady={_isWheelSystemReady}");
#endif
            return;
        }

        if (DailyWheelSystem.Instance.ShouldAutoShowToday())
        {
            DailyWheelSystem.Instance.MarkAutoShowShownToday();
            _isWaitingForWheel = true;
            UIEvents.RequestShowDailyWheelModal();
            return;
        }

        if (DailyRewardSystem.Instance.ShouldAutoShowToday())
        {
            DailyRewardSystem.Instance.MarkAutoShowShownToday();
            _isWaitingForReward = true;
            UIEvents.RequestShowDailyRewardModal();
            return;
        }

        CompleteSequence();
    }

    private void CompleteSequence()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _isWaitingForWheel = false;
        _isWaitingForWheelClose = false;
        _isWaitingForReward = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[DailyStartupSequence] Signal -> StartupSequenceCompleted");
#endif
        UIEvents.RaiseStartupSequenceCompleted();
    }

    private void SyncSystemReadiness(string reason)
    {
        bool rewardReadyNow = DailyRewardSystem.Instance != null && DailyRewardSystem.Instance.IsBootstrapped;
        bool wheelReadyNow = DailyWheelSystem.Instance != null && DailyWheelSystem.Instance.IsBootstrapped;

        if (_isRewardSystemReady != rewardReadyNow || _isWheelSystemReady != wheelReadyNow)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[DailyStartupSequence] Readiness -> rewardReady={rewardReadyNow} | wheelReady={wheelReadyNow} | Reason={reason}");
#endif
        }

        _isRewardSystemReady = rewardReadyNow;
        _isWheelSystemReady = wheelReadyNow;
    }
}
