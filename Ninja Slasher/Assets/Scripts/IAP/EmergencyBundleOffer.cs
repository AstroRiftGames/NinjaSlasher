using UnityEngine;

public class EmergencyBundleOffer
{
    public BundleTier Tier;
    public string ProductId;
    public string LocalizedPrice;

    public string DisplayName;

    public Sprite Icon;

    public BundleRewardData Reward;

    public float OfferDurationSeconds;
    public LevelFailedContext FailContext;
}
