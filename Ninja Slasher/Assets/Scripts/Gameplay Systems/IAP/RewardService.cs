using UnityEngine;

public readonly struct IapGrantRuntimeEffects
{
    public readonly bool CoinsChanged;
    public readonly bool AdsRemoved;
    public readonly bool PowerUpsChanged;
    public readonly bool LivesChanged;

    public IapGrantRuntimeEffects(bool coinsChanged, bool adsRemoved, bool powerUpsChanged, bool livesChanged)
    {
        CoinsChanged = coinsChanged;
        AdsRemoved = adsRemoved;
        PowerUpsChanged = powerUpsChanged;
        LivesChanged = livesChanged;
    }
}

public class RewardService : MonoBehaviourSingleton<RewardService>
{
    private int _grantCounter = 0;

    public void Grant(StoreProductDefinition product)
    {
        _grantCounter++;

        if (product == null)
        {
            Debug.LogWarning("[RewardService] Grant called with null product.");
            return;
        }

        switch (product.rewardType)
        {
            case RewardType.Bundle:
                GrantBundleReward(product.bundleReward);
                break;

            case RewardType.Coins:
                GrantCoins(product.coinAmount);
                break;

            case RewardType.RemoveAds:
                GrantRemoveAds();
                break;
        }

        GameEvents.RaiseRewardGranted(product);
    }

    public bool TryGrantDurably(StoreProductDefinition product, string purchaseKey, out string error)
    {
        error = null;

        if (product == null)
        {
            error = "Product definition is null.";
            return false;
        }

        if (SaveManager.Instance == null)
        {
            error = "SaveManager instance is not available.";
            return false;
        }

        if (!SaveManager.Instance.TryApplyIapGrant(product, purchaseKey, out IapGrantRuntimeEffects runtimeEffects, out error))
            return false;

        ApplyRuntimeEffects(product, runtimeEffects);
        GameEvents.RaiseRewardGranted(product);
        return true;
    }

    private void GrantBundleReward(BundleRewardData reward)
    {
        if (reward == null) return;

        // Lives
        if (reward.unlimitedLives && reward.unlimitedLivesDurationMinutes > 0f)
        {
            LifeManager.Instance?.ActivateUnlimitedLives(reward.unlimitedLivesDurationMinutes);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[RewardService] Unlimited lives: {reward.unlimitedLivesDurationMinutes} min");
#endif
        }
        else if (reward.regularLivesCount > 0)
        {
            for (int i = 0; i < reward.regularLivesCount; i++)
                LifeManager.Instance?.GrantExternalLife(LifeRestoreSource.IapPurchase);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[RewardService] +{reward.regularLivesCount} lives | Total={LifeManager.Instance?.GetRealLives()}");
#endif
        }

        // Power-ups
        if (reward.powerUps != null)
        {
            foreach (var entry in reward.powerUps)
            {
                if (entry.quantity <= 0) continue;
                AutoSaveManager.Instance?.OnPowerUpObtained(entry.type, entry.quantity);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[RewardService] +{entry.quantity}x {entry.type}");
#endif
            }
        }

        // Coins
        if (reward.coins > 0)
            GrantCoins(reward.coins);
    }

    private void GrantCoins(int amount)
    {
        if (amount <= 0) return;
        SaveManager.Instance?.AddCoins(amount);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[RewardService] +{amount} coins | Wallet={SaveManager.Instance?.GetCoins()}");
#endif
    }

    private void GrantRemoveAds()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[RewardService] SaveManager not available. Remove Ads could not be persisted.");
            return;
        }

        SaveManager.Instance.SetAdsRemoved(true);
        GameEvents.RaiseAdsRemoved();
    }

    private static void ApplyRuntimeEffects(StoreProductDefinition product, IapGrantRuntimeEffects runtimeEffects)
    {
        if (runtimeEffects.CoinsChanged && SaveManager.Instance != null)
            GameEvents.RaiseCoinsChanged(SaveManager.Instance.GetCoins());

        if (runtimeEffects.AdsRemoved)
            GameEvents.RaiseAdsRemoved();

        if (runtimeEffects.PowerUpsChanged)
            PowerUpManager.Instance?.ReloadFromSave();

        if (runtimeEffects.LivesChanged)
            LifeManager.Instance?.ReloadFromSaveForExternalGrant();

        Debug.Log($"[RewardService] Durable IAP reward applied | productId='{product?.PrimaryProductId}' | coinsChanged={runtimeEffects.CoinsChanged} | adsRemoved={runtimeEffects.AdsRemoved} | powerUpsChanged={runtimeEffects.PowerUpsChanged} | livesChanged={runtimeEffects.LivesChanged}");
    }
}
