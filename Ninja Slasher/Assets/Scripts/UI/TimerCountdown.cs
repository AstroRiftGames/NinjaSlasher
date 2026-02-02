using UnityEngine;

public class TimerCountdownAudio : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioEvent countdownTickEvent;

    [Header("Countdown Settings")]
    [SerializeField] private float countdownThreshold = 5f;

    private int lastSecondTicked = -1;
    private bool isInCountdown = false;

    #region UNITY LIFECYCLE

    private void OnEnable()
    {
        GameEvents.OnLevelTimeChanged += HandleTimerUpdate;
        GameEvents.OnLevelTimeExpired += ResetCountdown;
        GameEvents.OnLevelStarted += ResetCountdown;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelTimeChanged -= HandleTimerUpdate;
        GameEvents.OnLevelTimeExpired -= ResetCountdown;
        GameEvents.OnLevelStarted -= ResetCountdown;
    }

    #endregion

    #region EVENT HANDLERS

    private void HandleTimerUpdate(float timeRemaining)
    {
        if (timeRemaining > countdownThreshold)
        {
            if (isInCountdown)
            {
                isInCountdown = false;
                lastSecondTicked = -1;
            }
            return;
        }

        if (!isInCountdown)
        {
            isInCountdown = true;
        }

        int currentSecond = Mathf.CeilToInt(timeRemaining);

        if (currentSecond != lastSecondTicked && currentSecond > 0)
        {
            PlayCountdownTick();
            lastSecondTicked = currentSecond;
        }
    }

    private void ResetCountdown()
    {
        lastSecondTicked = -1;
        isInCountdown = false;
    }

    #endregion

    #region AUDIO PLAYBACK

    private void PlayCountdownTick()
    {
        if (countdownTickEvent == null)
        {
            return;
        }

        if (AudioService.Instance == null)
        {
            return;
        }

        AudioService.Instance.PlaySFX(countdownTickEvent);
    }

    #endregion

#if UNITY_EDITOR
    [ContextMenu("Debug/Simular Countdown (5s)")]
    private void DebugSimulateCountdown()
    {
        HandleTimerUpdate(5f);
    }

    [ContextMenu("Debug/Simular Tick")]
    private void DebugSimulateTick()
    {
        PlayCountdownTick();
    }
#endif
}