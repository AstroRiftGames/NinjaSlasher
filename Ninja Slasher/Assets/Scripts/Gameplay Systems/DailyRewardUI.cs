using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DailyRewardUI : MonoBehaviour
{
    [Header("Panel Principal")]
    public GameObject dailyRewardPanel;
    public Button closeButton;
    public TextMeshProUGUI nextRewardTimeText;
    public DailyRewardDayUI[] weeklyRewardDays = new DailyRewardDayUI[7];
    public Button claimButton;
    public TextMeshProUGUI claimButtonText;

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

        SetupButtons();

        SubscribeToEvents();
        InitializeUI();

        if (dailyRewardPanel != null)
            dailyRewardPanel.SetActive(false);
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    void Update()
    {
        UpdateNextRewardTimer();
    }

    void SubscribeToEvents()
    {
        DailyRewardSystem.OnRewardClaimed += OnRewardClaimed;
        DailyRewardSystem.OnRewardAvailabilityChanged += OnRewardAvailabilityChanged;
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
        UpdateAllUI();
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

    public void ShowDailyRewardPanel()
    {
        if (dailyRewardPanel == null)
        {
            return;
        }

        Debug.Log("[DailyRewardUI] Activando panel");
        dailyRewardPanel.SetActive(true);
        UpdateAllUI();
    }

    public void HideDailyRewardPanel()
    {
        if (dailyRewardPanel != null)
        {
            dailyRewardPanel.SetActive(false);
        }
    }

    void UpdateAllUI()
    {
        if (!isInitialized || dailyRewardSystem == null) return;

        UpdateWeeklyProgress();
        UpdateClaimButton();
    }

    void SetupButtons()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(() => {dailyRewardSystem.ClaimReward();});

        if (closeButton != null)
            closeButton.onClick.AddListener(HideDailyRewardPanel);
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
            nextRewardTimeText.text = timeText.Contains("Disponible") ? timeText : $"Next reward: {timeText}";
        }
    }

    void OnRewardClaimed(DailyReward reward)
    {
        UpdateAllUI();
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
            ShowDailyRewardPanel();
        }
    }

    public bool HasAvailableReward()
    {
        return dailyRewardSystem != null && dailyRewardSystem.CanClaimToday();
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
            rewardIcon.sprite = reward.icon;

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
        if (quantityText != null) quantityText.color = textColor;
        if (rewardNameText != null) rewardNameText.color = textColor;
    }

    string GetDayName(int dayIndex)
    {
        string[] dayNames = { "HOY", "DÍA 2", "DÍA 3", "DÍA 4", "DÍA 5", "DÍA 6", "DÍA 7" };
        return dayIndex < dayNames.Length ? dayNames[dayIndex] : $"DÍA {dayIndex + 1}";
    }
}