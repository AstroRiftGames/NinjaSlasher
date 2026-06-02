using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DailyRewardUIManager : MonoBehaviourSingleton<DailyRewardUIManager>
{
    private static readonly WaitForSecondsRealtime RewardUiTickDelay = new(1f);

    public TextMeshProUGUI nextRewardTimeText;
    public DailyRewardDayUI[] weeklyRewardDays = new DailyRewardDayUI[7];

    [SerializeField] private Button closeButton;
    [SerializeField] private Button claimButton;
    public TextMeshProUGUI claimButtonText;

    [Header("Double Reward Button")]
    [SerializeField] private Button _doubleDailyRewardButton;
    [SerializeField] private TextMeshProUGUI _doubleRewardButtonText;

    [Header("DAY LABEL COLORS")]
    public Color availableColor;
    public Color claimedColor;
    public Color lockedColor;
    public Color todayColor;

    private DailyRewardSystem dailyRewardSystem;
    private bool isInitialized = false;
    private bool _eventsSubscribed;
    private Coroutine _rewardUiTickRoutine;
    private bool? _lastCanClaimToday;
    private string _lastNextRewardTimerText;
    private bool? _lastDoubleRewardInteractable;
    private string _lastDoubleRewardButtonText;
    private string _lastClaimButtonText;
    private bool? _lastClaimButtonInteractable;
    private bool _awaitingBootstrap;
    private bool _isDoubleRewardFlowInProgress;

    private UIAudioContext _audioContext;

    public override void Awake()
    {
        base.Awake();
        ResolveDependencies();
    }

    void OnEnable()
    {
        ResolveDependencies();
        HookButtons();
        SubscribeToEvents();
        DailyRewardSystem.OnBootstrapped += OnRewardSystemBootstrapped;
        TryBootstrapAndRefresh("OnEnable");
        StartRewardUiTick();
    }

    void OnDisable()
    {
        StopRewardUiTick();
        DailyRewardSystem.OnBootstrapped -= OnRewardSystemBootstrapped;
        UnsubscribeFromEvents();
    }

    protected override void OnDestroy()
    {
        StopRewardUiTick();
        DailyRewardSystem.OnBootstrapped -= OnRewardSystemBootstrapped;
        UnsubscribeFromEvents();
        base.OnDestroy();
    }

    private void ResolveDependencies()
    {
        if (dailyRewardSystem == null)
            dailyRewardSystem = DailyRewardSystem.Instance;

        if (_audioContext == null)
            _audioContext = GetComponent<UIAudioContext>();
    }

    private void HookButtons()
    {
        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(OnClaimPressed);
            claimButton.onClick.AddListener(OnClaimPressed);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnClosePressed);
            closeButton.onClick.AddListener(OnClosePressed);
        }

        if (_doubleDailyRewardButton != null)
        {
            _doubleDailyRewardButton.onClick.RemoveListener(OnDoubleRewardPressed);
            _doubleDailyRewardButton.onClick.AddListener(OnDoubleRewardPressed);
        }
    }

    void SubscribeToEvents()
    {
        if (_eventsSubscribed)
            return;

        GameEvents.OnRewardClaimed += OnRewardClaimed;
        GameEvents.OnRewardAvailabilityChanged += OnRewardAvailabilityChanged;
        GameEvents.OnRewardDoubled += OnRewardDoubled;

        if (AdsManager.Instance != null)
            AdsManager.Instance.OnRewardedAdFlowCompleted += OnRewardedAdFlowCompleted;

        _eventsSubscribed = true;
    }

    void UnsubscribeFromEvents()
    {
        if (!_eventsSubscribed)
            return;

        GameEvents.OnRewardClaimed -= OnRewardClaimed;
        GameEvents.OnRewardAvailabilityChanged -= OnRewardAvailabilityChanged;
        GameEvents.OnRewardDoubled -= OnRewardDoubled;

        if (AdsManager.Instance != null)
            AdsManager.Instance.OnRewardedAdFlowCompleted -= OnRewardedAdFlowCompleted;

        _eventsSubscribed = false;
    }

    void EnsureInitialized()
    {
        if (dailyRewardSystem == null) return;
        if (isInitialized) return;

        SetupWeeklyRewards();
        isInitialized = true;
    }

    void SetupWeeklyRewards()
    {
        for (int i = 0; i < weeklyRewardDays.Length && i < dailyRewardSystem.weeklyRewards.Length; i++)
        {
            if (weeklyRewardDays[i] != null)
            {
                weeklyRewardDays[i].SetupDay(i, dailyRewardSystem.weeklyRewards[i]);
            }
        }
    }

    public void ShowDailyReward()
    {
        if (!TryBootstrapAndRefresh("ShowDailyReward"))
            return;
    }

    void UpdateWeeklyProgress(string reason)
    {
        if (dailyRewardSystem == null) return;

        DailyAvailabilitySnapshot availability = dailyRewardSystem.GetAvailabilitySnapshot();
        bool[] claimedDays = dailyRewardSystem.GetWeeklyProgress();
        int currentDay = dailyRewardSystem.GetCurrentWeekDay();
        bool canClaimToday = availability.IsAvailable;

        for (int i = 0; i < weeklyRewardDays.Length; i++)
        {
            if (weeklyRewardDays[i] != null)
            {
                DayState state = GetDayState(i, currentDay, claimedDays[i], canClaimToday);
                Color labelColor = GetColorForState(state);
                weeklyRewardDays[i].UpdateDayState(state, labelColor);
            }
        }

    }

    DayState GetDayState(int dayIndex, int currentDay, bool isClaimed, bool canClaimToday)
    {
        if (dayIndex == currentDay && canClaimToday)
            return DayState.Available;
        else if (isClaimed)
            return DayState.Claimed;
        else if (dayIndex < currentDay)
            return DayState.Missed;
        else
            return DayState.Locked;
    }

    Color GetColorForState(DayState state)
    {
        switch (state)
        {
            case DayState.Available:
                return todayColor;
            case DayState.Claimed:
                return claimedColor;
            case DayState.Locked:
                return lockedColor;
            case DayState.Missed:
                return Color.red;
            default:
                return Color.white;
        }
    }

    void UpdateClaimButton(bool canClaim, string reason, bool force = false)
    {
        if (claimButton == null) return;

        bool interactableChanged = force || !_lastClaimButtonInteractable.HasValue || _lastClaimButtonInteractable.Value != canClaim;
        string buttonText = canClaim ? "RECLAMAR" : "RECLAMADO";
        bool textChanged = force || !string.Equals(_lastClaimButtonText, buttonText, StringComparison.Ordinal);

        if (interactableChanged)
            claimButton.interactable = canClaim;

        if (claimButtonText != null)
        {
            if (textChanged)
                claimButtonText.text = buttonText;
        }

        _lastClaimButtonInteractable = canClaim;
        _lastClaimButtonText = buttonText;
    }

    void UpdateNextRewardTimer(string reason, bool force = false)
    {
        if (nextRewardTimeText != null && dailyRewardSystem != null)
        {
            DailyAvailabilitySnapshot availability = dailyRewardSystem.GetAvailabilitySnapshot();
            string nextRewardText = DailyAvailabilityUIFormatter.FormatLockedAvailability(
                "Proxima recompensa: ",
                availability.NextAvailabilityUtc,
                "DISPONIBLE AHORA");

            bool timerChanged = force || !string.Equals(_lastNextRewardTimerText, nextRewardText, StringComparison.Ordinal);
            if (!timerChanged)
                return;

            nextRewardTimeText.text = nextRewardText;
            _lastNextRewardTimerText = nextRewardText;
        }
    }

    void OnRewardClaimed(DailyReward reward)
    {
        RefreshVisibleState("GameEvents.OnRewardClaimed", force: true);

        if (_audioContext != null && _audioContext.Audio != null)
        {
            AudioService.Instance.PlaySFX(_audioContext.Audio.rewardPrize);
        }

        StartCoroutine(ShowRewardClaimedFeedback(reward));
    }

    void OnRewardAvailabilityChanged(bool isAvailable)
    {
        RefreshAvailabilityState(isAvailable, "GameEvents.OnRewardAvailabilityChanged", force: true);
    }

    IEnumerator ShowRewardClaimedFeedback(DailyReward reward)
    {
        yield return new WaitForSeconds(1f);
    }

    public void CheckAndShowDailyRewardOnGameStart()
    {
        if (!TryBootstrapAndRefresh("CheckAndShowDailyRewardOnGameStart"))
            return;

        if (dailyRewardSystem != null && dailyRewardSystem.CanClaimToday())
        {
        }
    }

    public bool HasAvailableReward()
    {
        return dailyRewardSystem != null && dailyRewardSystem.CanClaimToday();
    }

    private void OnClaimPressed()
    {
        if (dailyRewardSystem == null) return;
        if (!dailyRewardSystem.CanClaimToday()) return;

        if (dailyRewardSystem.ClaimReward())
        {
        }
    }

    private void OnClosePressed()
    {
        //UIManager.Instance.HideDailyRewardModal();
        UIEvents.RequestHideDailyRewardModal();
    }

    private void OnRewardDoubled()
    {
        UpdateDoubleRewardButton("GameEvents.OnRewardDoubled", force: true);
    }

    void UpdateDoubleRewardButton(string reason, bool force = false)
    {
        if (_doubleDailyRewardButton == null || dailyRewardSystem == null) return;

        bool canDouble = dailyRewardSystem.CanDoubleToday() &&
                        AdsManager.Instance != null &&
                        AdsManager.Instance.IsRewardedAdReady();

        bool hasDoubledToday = dailyRewardSystem.HasDoubledToday();
        string buttonText;

        if (hasDoubledToday)
        {
            buttonText = "DUPLICADA!";
        }
        else if (canDouble)
        {
            buttonText = "VER ANUNCIO x2";
        }
        else if (AdsManager.Instance != null && !AdsManager.Instance.IsRewardedAdReady())
        {
            buttonText = "CARGANDO...";
        }
        else
        {
            buttonText = "NO DISPONIBLE";
        }

        bool interactableChanged = force || !_lastDoubleRewardInteractable.HasValue || _lastDoubleRewardInteractable.Value != canDouble;
        bool textChanged = force || !string.Equals(_lastDoubleRewardButtonText, buttonText, StringComparison.Ordinal);

        if (interactableChanged)
            _doubleDailyRewardButton.interactable = canDouble;

        if (_doubleRewardButtonText != null)
        {
            if (textChanged)
                _doubleRewardButtonText.text = buttonText;
        }

        _lastDoubleRewardInteractable = canDouble;
        _lastDoubleRewardButtonText = buttonText;
    }

    private void OnDoubleRewardPressed()
    {
        if (_isDoubleRewardFlowInProgress) return;
        if (dailyRewardSystem == null || AdsManager.Instance == null) return;

        if (!dailyRewardSystem.CanDoubleToday())
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("[DailyRewardUIManager] No se puede duplicar la recompensa hoy");
#endif
            return;
        }

        if (!AdsManager.Instance.IsRewardedAdReady())
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("[DailyRewardUIManager] Anuncio no esta listo");
#endif
            return;
        }

        _isDoubleRewardFlowInProgress = true;
        if (_doubleDailyRewardButton != null)
            _doubleDailyRewardButton.interactable = false;

        AdsManager.Instance.ShowRewardedAdForDoubleDailyReward();
    }

    private void OnRewardedAdFlowCompleted(string context, bool rewarded)
    {
        if (string.Equals(context, "double_daily_reward", StringComparison.Ordinal))
        {
            _isDoubleRewardFlowInProgress = false;
            UpdateDoubleRewardButton("OnRewardedAdFlowCompleted", force: true);
        }
    }

    private void OnRewardSystemBootstrapped()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[DailyRewardUIManager] Signal -> DailyRewardSystem.OnBootstrapped");
#endif
        TryBootstrapAndRefresh("DailyRewardSystem.OnBootstrapped");
    }

    private void RefreshVisibleState(string reason, bool force = false)
    {
        if (!isInitialized || dailyRewardSystem == null)
            return;

        DailyAvailabilitySnapshot availability = dailyRewardSystem.GetAvailabilitySnapshot();
        RefreshAvailabilityState(availability.IsAvailable, reason, force);
        UpdateDoubleRewardButton(reason, force);
    }

    private void RefreshAvailabilityState(bool canClaimToday, string reason, bool force = false)
    {
        bool availabilityChanged = force || !_lastCanClaimToday.HasValue || _lastCanClaimToday.Value != canClaimToday;

        if (availabilityChanged)
            UpdateWeeklyProgress(reason);

        UpdateClaimButton(canClaimToday, reason, force || availabilityChanged);
        UpdateNextRewardTimer(reason, force || availabilityChanged);

        _lastCanClaimToday = canClaimToday;
    }

    private void StartRewardUiTick()
    {
        if (_rewardUiTickRoutine != null)
            return;

        _rewardUiTickRoutine = StartCoroutine(RewardUiTickLoop());
    }

    private void StopRewardUiTick()
    {
        if (_rewardUiTickRoutine == null)
            return;

        StopCoroutine(_rewardUiTickRoutine);
        _rewardUiTickRoutine = null;
    }

    private IEnumerator RewardUiTickLoop()
    {
        while (enabled)
        {
            yield return RewardUiTickDelay;

            if (!IsRewardSystemReady())
                continue;

            if (!isInitialized || dailyRewardSystem == null)
                continue;

            DailyAvailabilitySnapshot availability = dailyRewardSystem.GetAvailabilitySnapshot();
            RefreshAvailabilityState(availability.IsAvailable, "RewardUiTick");
            UpdateNextRewardTimer("RewardUiTick");
        }
    }

    private bool TryBootstrapAndRefresh(string reason)
    {
        ResolveDependencies();

        if (!IsRewardSystemReady())
        {
            _awaitingBootstrap = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[DailyRewardUIManager] Bootstrap -> Waiting | Reason={reason} | systemExists={dailyRewardSystem != null} | saveLoaded={SaveManager.Instance != null && SaveManager.Instance.IsDataLoaded} | rewardBootstrapped={dailyRewardSystem != null && dailyRewardSystem.IsBootstrapped}");
#endif
            return false;
        }

        bool wasAwaitingBootstrap = _awaitingBootstrap;
        EnsureInitialized();
        RefreshVisibleState(reason, force: true);
        _awaitingBootstrap = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[DailyRewardUIManager] Bootstrap -> Ready | Reason={reason} | initialized={isInitialized} | recoveredFromWaiting={wasAwaitingBootstrap}");
#endif
        return true;
    }

    private bool IsRewardSystemReady()
    {
        if (dailyRewardSystem == null)
            return false;

        if (SaveManager.Instance == null || !SaveManager.Instance.IsDataLoaded)
            return false;

        return dailyRewardSystem.IsBootstrapped;
    }
}

public enum DayState
{
    Available,
    Claimed,
    Locked,
    Missed
}

[System.Serializable]
public class DailyRewardDayUI
{
    [Header("UI REFERENCES")]
    public GameObject dayContainer;
    public TextMeshProUGUI dayLabel;
    public Image rewardIcon;
    public TextMeshProUGUI quantityText;
    public Image backgroundImage;

    private int dayIndex;
    private DailyReward reward;

    public void SetupDay(int index, DailyReward dailyReward)
    {
        dayIndex = index;
        reward = dailyReward;

        if (dayLabel != null)
            dayLabel.text = GetDayName(index);

        if (rewardIcon != null && reward.icon != null)
        {
            rewardIcon.sprite = reward.icon;
            rewardIcon.preserveAspect = true;
        }

        if (quantityText != null)
            quantityText.text = $"x{reward.quantity}";
    }

    public void UpdateDayState(DayState state, Color labelColor)
    {
        if (dayLabel != null)
        {
            dayLabel.color = labelColor;
        }

        switch (state)
        {
            case DayState.Available:
                SetAvailableState();
                break;
            case DayState.Claimed:
                SetClaimedState();
                break;
            case DayState.Locked:
                SetLockedState();
                break;
            case DayState.Missed:
                SetMissedState();
                break;
        }
    }

    void SetAvailableState()
    {
        SetElementsActive(true);
    }

    void SetClaimedState()
    {
        SetElementsActive(true);
    }

    void SetLockedState()
    {
        SetElementsActive(true);
        SetElementsAlpha(0.5f);
    }

    void SetMissedState()
    {
        SetElementsActive(true);
        SetElementsAlpha(0.5f);
    }

    void SetElementsActive(bool active)
    {
        Color textColor = active ? Color.white : Color.gray;

        if (rewardIcon != null) rewardIcon.color = active ? Color.white : Color.gray;
        if (quantityText != null) quantityText.color = textColor;
    }

    void SetElementsAlpha(float alpha)
    {
        if (rewardIcon != null)
        {
            Color iconColor = rewardIcon.color;
            rewardIcon.color = new Color(iconColor.r, iconColor.g, iconColor.b, alpha);
        }

        if (quantityText != null)
        {
            Color quantityColor = quantityText.color;
            quantityText.color = new Color(quantityColor.r, quantityColor.g, quantityColor.b, alpha);
        }
    }

    string GetDayName(int dayIndex)
    {
        string[] dayNames = { "DIA 1", "DIA 2", "DIA 3", "DIA 4", "DIA 5", "DIA 6", "DIA 7" };
        return dayIndex < dayNames.Length ? dayNames[dayIndex] : $"DIA {dayIndex + 1}";
    }
}

internal static class DailyAvailabilityUIFormatter
{
    private const string CountdownFormat = @"hh\:mm\:ss";

    public static string FormatRewardAvailability(string countdownText, string lockedPrefix, string availableText)
    {
        if (string.IsNullOrWhiteSpace(countdownText))
            return availableText;

        if (countdownText.IndexOf("DISPONIBLE", StringComparison.OrdinalIgnoreCase) >= 0)
            return availableText;

        if (!TimeSpan.TryParseExact(countdownText, CountdownFormat, CultureInfo.InvariantCulture, out TimeSpan remaining))
            return $"{lockedPrefix}{countdownText}";

        DateTime nextAvailabilityUtc = DateTime.UtcNow.Add(remaining);
        return FormatLockedAvailability(lockedPrefix, nextAvailabilityUtc, availableText);
    }

    public static string FormatLockedAvailability(string lockedPrefix, DateTime nextAvailabilityUtc, string availableText)
    {
        DateTime normalizedUtc = NormalizeUtc(nextAvailabilityUtc);
        TimeSpan remaining = normalizedUtc - DateTime.UtcNow;

        if (remaining.TotalSeconds <= 0d)
            return availableText;

        return $"{lockedPrefix}{FormatCountdown(remaining)}";
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
            return value;

        if (value.Kind == DateTimeKind.Local)
            return value.ToUniversalTime();

        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static string FormatCountdown(TimeSpan remaining)
    {
        int totalHours = Math.Max(0, (int)Math.Floor(remaining.TotalHours));
        return $"{totalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
    }
}
