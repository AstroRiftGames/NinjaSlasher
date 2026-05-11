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
    private bool _isWheelSystemReady;

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

    private void OnRewardSystemBootstrapped()
    {
        _isRewardSystemReady = true;
        Debug.Log("[DailyStartupSequence] Signal -> DailyRewardSystem.OnBootstrapped");
        TryAdvanceSequence();
    }

    private void OnWheelSystemBootstrapped()
    {
        _isWheelSystemReady = true;
        Debug.Log("[DailyStartupSequence] Signal -> DailyWheelSystem.OnBootstrapped");
        TryAdvanceSequence();
    }

    private void OnLevelSelectorReady()
    {
        SyncSystemReadiness("OnLevelSelectorReady");
        _isLevelSelectorReady = true;
        _isRunning = true;
        _isWaitingForWheel = false;
        _isWaitingForWheelClose = false;
        _isWaitingForReward = false;

        Debug.Log("[DailyStartupSequence] Signal -> StartupSequenceStarted");
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
            Debug.Log($"[DailyStartupSequence] Waiting -> rewardReady={_isRewardSystemReady} | wheelReady={_isWheelSystemReady}");
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

        Debug.Log("[DailyStartupSequence] Signal -> StartupSequenceCompleted");
        UIEvents.RaiseStartupSequenceCompleted();
    }

    private void SyncSystemReadiness(string reason)
    {
        bool rewardReadyNow = DailyRewardSystem.Instance != null && DailyRewardSystem.Instance.IsBootstrapped;
        bool wheelReadyNow = DailyWheelSystem.Instance != null && DailyWheelSystem.Instance.IsBootstrapped;

        if (_isRewardSystemReady != rewardReadyNow || _isWheelSystemReady != wheelReadyNow)
        {
            Debug.Log($"[DailyStartupSequence] Readiness -> rewardReady={rewardReadyNow} | wheelReady={wheelReadyNow} | Reason={reason}");
        }

        _isRewardSystemReady = rewardReadyNow;
        _isWheelSystemReady = wheelReadyNow;
    }
}
