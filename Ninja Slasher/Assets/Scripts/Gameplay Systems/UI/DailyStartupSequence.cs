using System;
using UnityEngine;

public sealed class DailyStartupSequence : IDisposable
{
    private bool _isLevelSelectorReady;
    private bool _isWaitingForWheel;
    private bool _isWaitingForWheelClose;
    private bool _isWaitingForReward;
    private bool _isRunning;

    public DailyStartupSequence(MonoBehaviour runner)
    {
        SaveManager.OnDataLoaded += OnDataLoaded;
        UIEvents.OnLevelSelectorReady += OnLevelSelectorReady;
        UIEvents.OnWheelSequenceCompleted += OnWheelSequenceCompleted;
        UIEvents.OnDailyWheelModalClosed += OnDailyWheelModalClosed;
        UIEvents.OnDailyRewardModalClosed += OnDailyRewardModalClosed;
    }

    public void Dispose()
    {
        SaveManager.OnDataLoaded -= OnDataLoaded;
        UIEvents.OnLevelSelectorReady -= OnLevelSelectorReady;
        UIEvents.OnWheelSequenceCompleted -= OnWheelSequenceCompleted;
        UIEvents.OnDailyWheelModalClosed -= OnDailyWheelModalClosed;
        UIEvents.OnDailyRewardModalClosed -= OnDailyRewardModalClosed;
    }

    private void OnDataLoaded(GameData _)
    {
        TryAdvanceSequence();
    }

    private void OnLevelSelectorReady()
    {
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
        if (SaveManager.Instance == null || !SaveManager.Instance.IsDataLoaded) return;
        if (_isWaitingForWheel || _isWaitingForWheelClose || _isWaitingForReward) return;

        if (DailyWheelSystem.Instance == null || DailyRewardSystem.Instance == null)
        {
            CompleteSequence();
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
}
