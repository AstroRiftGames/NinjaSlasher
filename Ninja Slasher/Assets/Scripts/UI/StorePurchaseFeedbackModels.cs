using System.Collections.Generic;
using UnityEngine;

public enum StoreRewardFeedbackType
{
    Coins,
    UnlimitedLives
}

public sealed class StoreRewardFeedbackEntry
{
    public StoreRewardFeedbackType RewardType;
    public int Amount;
    public float DurationMinutes;
}

public sealed class StorePurchaseFeedbackRequest
{
    public string ProductId;
    public RectTransform SourceTransform;
    public List<StoreRewardFeedbackEntry> Rewards = new();

    public bool HasRewards => Rewards != null && Rewards.Count > 0;
}
