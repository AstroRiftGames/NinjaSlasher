using System;
using System.Collections;
using UnityEngine;

public sealed class DailyStartupSequence : IDisposable
{
    public static bool IsSequenceRunning { get; private set; }

    private bool _isLevelSelectorReady;
    private bool _isWaitingForWheel;
    private bool _isWaitingForReward;
    private bool _isRunning;

    private readonly MonoBehaviour _runner;
    private const float WheelToRewardDelay = 1.5f;

    public DailyStartupSequence(MonoBehaviour runner)
    {
        _runner = runner;
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
        IsSequenceRunning = true;
        _isWaitingForWheel = false;
        _isWaitingForReward = false;

        TryAdvanceSequence();
    }

    private void OnWheelSequenceCompleted()
    {
        if (!_isWaitingForWheel) return;

        _isWaitingForWheel = false;
        UIEvents.RequestHideDailyWheelModal();
        _runner.StartCoroutine(DelayedAdvanceSequence());
    }

    private IEnumerator DelayedAdvanceSequence()
    {
        yield return new WaitForSeconds(WheelToRewardDelay);
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
        IsSequenceRunning = false;
        _isWaitingForWheel = false;
        _isWaitingForReward = false;

        UIEvents.RaiseStartupSequenceCompleted();
    }
}
