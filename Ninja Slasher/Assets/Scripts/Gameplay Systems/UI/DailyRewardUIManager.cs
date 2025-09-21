using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DailyRewardUIManager : MonoBehaviourSingleton<DailyRewardUIManager>
{
    public TextMeshProUGUI nextRewardTimeText;
    public DailyRewardDayUI[] weeklyRewardDays = new DailyRewardDayUI[7];

    [SerializeField] private Button closeButton;

    [SerializeField] private Button claimButton;
    public TextMeshProUGUI claimButtonText;

    //[Header("Double Reward Button")]
    //[SerializeField] private Button _doubleDailyRewardButton;
    //[SerializeField] private TextMeshProUGUI _doubleRewardButtonText;

    [Header("BACKGROUND COLORS")]
    public Color availableColor = Color.white;
    public Color claimedColor = Color.green;
    public Color lockedColor = Color.gray;
    public Color todayColor = Color.yellow;

    private DailyRewardSystem dailyRewardSystem;
    private bool isInitialized = false;

    void Start()
    {
        dailyRewardSystem = DailyRewardSystem.Instance;

        SubscribeToEvents();
        InitializeUI();
    }

    void OnEnable()
    {
        if (dailyRewardSystem == null) dailyRewardSystem = DailyRewardSystem.Instance;

        HookButtons();
        SubscribeToEvents();
        if (!isInitialized) InitializeUI();
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    void Update()
    {
        UpdateNextRewardTimer();
    }

    private void HookButtons()
    {
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimPressed);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnClosePressed);
        }

        //if (_doubleDailyRewardButton != null)
        //{
        //    _doubleDailyRewardButton.onClick.RemoveAllListeners();
        //    _doubleDailyRewardButton.onClick.AddListener(OnDoubleRewardPressed);
        //}
    }

    void SubscribeToEvents()
    {
        DailyRewardSystem.OnRewardClaimed += OnRewardClaimed;
        DailyRewardSystem.OnRewardAvailabilityChanged += OnRewardAvailabilityChanged;
        DailyRewardSystem.OnRewardDoubled += OnRewardDoubled;
    }

    void UnsubscribeFromEvents()
    {
        DailyRewardSystem.OnRewardClaimed -= OnRewardClaimed;
        DailyRewardSystem.OnRewardAvailabilityChanged -= OnRewardAvailabilityChanged;
    }

    void InitializeUI()
    {
        if (dailyRewardSystem == null) return;

        SetupWeeklyRewards();
        ShowDailyReward();
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
        if (!isInitialized || dailyRewardSystem == null) return;

        UpdateWeeklyProgress();
        UpdateClaimButton();
        UpdateDoubleRewardButton();
    }

    void UpdateWeeklyProgress()
    {
        if (dailyRewardSystem == null) return;

        bool[] claimedDays = dailyRewardSystem.GetWeeklyProgress();
        int currentDay = dailyRewardSystem.GetCurrentWeekDay();
        bool canClaimToday = dailyRewardSystem.CanClaimToday();

        for (int i = 0; i < weeklyRewardDays.Length; i++)
        {
            if (weeklyRewardDays[i] != null)
            {
                DayState state = GetDayState(i, currentDay, claimedDays[i], canClaimToday);
                weeklyRewardDays[i].UpdateDayState(state);
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

    void UpdateClaimButton()
    {
        if (claimButton == null || dailyRewardSystem == null) return;

        bool canClaim = dailyRewardSystem.CanClaimToday();

        claimButton.interactable = canClaim;

        if (claimButtonText != null)
        {
            claimButtonText.text = canClaim ? "CLAIM" : "CLAIMED";
        }
    }

    void UpdateNextRewardTimer()
    {
        if (nextRewardTimeText != null && dailyRewardSystem != null)
        {
            string timeText = dailyRewardSystem.GetTimeUntilNextReward();
            nextRewardTimeText.text = timeText.Contains("AVAILABLE") ? timeText : $"Next reward: {timeText}";
        }
    }

    void OnRewardClaimed(DailyReward reward)
    {
        ShowDailyReward();
        StartCoroutine(ShowRewardClaimedFeedback(reward));
    }

    void OnRewardAvailabilityChanged(bool isAvailable)
    {
        UpdateClaimButton();
    }

    IEnumerator ShowRewardClaimedFeedback(DailyReward reward)
    {
        yield return new WaitForSeconds(1f);
    }

    public void CheckAndShowDailyRewardOnGameStart()
    {
        if (dailyRewardSystem != null && dailyRewardSystem.CanClaimToday())
        {
            ShowDailyReward();
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
            ShowDailyReward();
            var preGame = FindObjectOfType<PreGameUIManager>();
            if (preGame != null && preGame.isActiveAndEnabled)
                preGame.ShowPreGamePowerUps();
        }
    }

    private void OnClosePressed()
    {
        var canvasManager = GetComponentInParent<CanvasManager>();
        if (canvasManager != null) canvasManager.ShowHideDailyRewardCanvas();
    }

    private void OnAvailabilityChanged(bool canClaim)
    {
        if (claimButton) claimButton.interactable = canClaim;
    }

    private void OnRewardDoubled()
    {
        UpdateDoubleRewardButton();
    }

    void UpdateDoubleRewardButton()
    {
        //if (_doubleDailyRewardButton == null || dailyRewardSystem == null) return;

        //bool canDouble = dailyRewardSystem.CanDoubleToday() &&
        //                AdsManager.Instance != null &&
        //                AdsManager.Instance.IsRewardedAdReady();

        bool canDouble = dailyRewardSystem.CanDoubleToday();

        bool hasDoubledToday = dailyRewardSystem.HasDoubledToday();

        //_doubleDailyRewardButton.interactable = canDouble;

        //if (_doubleRewardButtonText != null)
        //{
        //    if (hasDoubledToday)
        //    {
        //        _doubleRewardButtonText.text = "DOUBLED!";
        //    }
        //    else if (canDouble)
        //    {
        //        _doubleRewardButtonText.text = "WATCH AD x2";
        //    }
        //    else if (AdsManager.Instance != null && !AdsManager.Instance.IsRewardedAdReady())
        //    {
        //        _doubleRewardButtonText.text = "LOADING...";
        //    }
        //    else
        //    {
        //        _doubleRewardButtonText.text = "UNAVAILABLE";
        //    }
        //}
    }


    //private void OnDoubleRewardPressed()
    //{
    //    if (dailyRewardSystem == null || AdsManager.Instance == null) return;

    //    if (!dailyRewardSystem.CanDoubleToday())
    //    {
    //        Debug.Log("No se puede duplicar la recompensa hoy");
    //        return;
    //    }

    //    if (!AdsManager.Instance.IsRewardedAdReady())
    //    {
    //        Debug.Log("Anuncio no está listo");
    //        return;
    //    }

    //    AdsManager.Instance.ShowRewardedAdForDoubleDailyReward();
    //}
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
    public TextMeshProUGUI rewardNameText;

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

        if (rewardNameText != null && !string.IsNullOrEmpty(reward.displayName))
            rewardNameText.text = reward.displayName;
    }

    public void UpdateDayState(DayState state)
    {
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
        if (backgroundImage != null) backgroundImage.color = Color.yellow;
    }

    void SetClaimedState()
    {
        SetElementsActive(true);
        if (backgroundImage != null) backgroundImage.color = Color.green;
    }

    void SetLockedState()
    {
        SetElementsActive(true);
        if (backgroundImage != null) backgroundImage.color = Color.gray;
    }

    void SetMissedState()
    {
        SetElementsActive(true);
        if (backgroundImage != null) backgroundImage.color = Color.red;
    }

    void SetElementsActive(bool active)
    {
        Color textColor = active ? Color.black : Color.gray;

        if (rewardIcon != null) rewardIcon.color = active ? Color.white : Color.gray;
        if (quantityText != null) quantityText.color = Color.white;
        if (rewardNameText != null) rewardNameText.color = textColor;
    }

    string GetDayName(int dayIndex)
    {
        string[] dayNames = { "TODAY", "DAY 2", "DAY 3", "DAY 4", "DAY 5", "DAY 6", "DAY 7" };
        return dayIndex < dayNames.Length ? dayNames[dayIndex] : $"DAY {dayIndex + 1}";
    }
}