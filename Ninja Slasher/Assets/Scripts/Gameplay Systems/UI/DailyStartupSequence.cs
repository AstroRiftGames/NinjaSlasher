using System;
using UnityEngine;

public sealed class DailyStartupSequence : IDisposable
{
    private bool _isLevelSelectorReady;
    private bool _isWaitingForWheel;
    private bool _isWaitingForReward;
    private bool _isRunning;

    public DailyStartupSequence()
    {
        SaveManager.OnDataLoaded += OnDataLoaded;
        UIEvents.OnLevelSelectorReady += OnLevelSelectorReady;
        UIEvents.OnWheelSequenceCompleted += OnWheelSequenceCompleted;
        UIEvents.OnDailyRewardModalClosed += OnDailyRewardModalClosed;
    }

    public void Dispose()
    {
        SaveManager.OnDataLoaded -= OnDataLoaded;
        UIEvents.OnLevelSelectorReady -= OnLevelSelectorReady;
        UIEvents.OnWheelSequenceCompleted -= OnWheelSequenceCompleted;
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
        _isWaitingForReward = false;

        TryAdvanceSequence();
    }

    private void OnWheelSequenceCompleted()
    {
        if (!_isWaitingForWheel) return;

        _isWaitingForWheel = false;
        UIEvents.RequestHideDailyWheelModal();
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
        if (_isWaitingForWheel || _isWaitingForReward) return;

        if (DailyWheelSystem.Instance == null || DailyRewardSystem.Instance == null)
        {
            Debug.LogWarning("[DailySequence] Daily systems not ready. Completing startup sequence to avoid blocking UI.");
            CompleteSequence();
            return;
        }

        if (DailyWheelSystem.Instance.CanSpinToday())
        {
            _isWaitingForWheel = true;
            Debug.Log("[DailySequence] Showing Daily Wheel");
            UIEvents.RequestShowDailyWheelModal();
            return;
        }

        if (DailyRewardSystem.Instance.CanClaimToday())
        {
            _isWaitingForReward = true;
            Debug.Log("[DailySequence] Showing Daily Reward");
            UIEvents.RequestShowDailyRewardModal();
            return;
        }

        Debug.Log("[DailySequence] No wheel or daily reward available");
        CompleteSequence();
    }

    private void CompleteSequence()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _isWaitingForWheel = false;
        _isWaitingForReward = false;

        Debug.Log("[DailySequence] Startup sequence completed");
        UIEvents.RaiseStartupSequenceCompleted();
    }
}
