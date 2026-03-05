using System;

public static class FrustrationEvaluator
{
    public static bool ShouldOffer(LevelFailedContext ctx,
                                   EmergencyBundleConfig config,
                                   GameData data)
    {
        if (config == null || data == null) return false;

        if (config.dailyCap > 0)
        {
            long nowDay  = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400;
            long lastDay = data.emergencyBundleLastActivationUtc / 86400;

            int usesToday = (data.emergencyBundleLastActivationUtc == 0 || nowDay != lastDay)
                            ? 0
                            : data.emergencyBundleUsesToday;

            if (usesToday >= config.dailyCap) return false;
        }

        if (config.cooldownHours > 0 && data.emergencyBundleLastActivationUtc > 0)
        {
            long elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                           - data.emergencyBundleLastActivationUtc;
            if (elapsed < (long)(config.cooldownHours * 3600)) return false;
        }

        if (ctx.IsBossLevel)
            return data.consecutiveBossLosses >= config.bossFailureThreshold;

        return ctx.ConsecutiveLosses >= config.consecutiveLossThreshold;
    }

    public static BundleTier SelectTier(LevelFailedContext ctx,
                                        EmergencyBundleConfig config,
                                        GameData data)
    {
        if (config.largeBundleOnBossWithNoLives
            && ctx.IsBossLevel
            && ctx.LivesRemaining == 0)
            return BundleTier.Large;

        int losses = ctx.IsBossLevel ? data.consecutiveBossLosses : ctx.ConsecutiveLosses;

        if (config.largeTierLossThreshold > 0 && losses >= config.largeTierLossThreshold)
            return BundleTier.Large;

        if (config.mediumTierLossThreshold > 0 && losses >= config.mediumTierLossThreshold)
            return BundleTier.Medium;

        return BundleTier.Small;
    }
}
