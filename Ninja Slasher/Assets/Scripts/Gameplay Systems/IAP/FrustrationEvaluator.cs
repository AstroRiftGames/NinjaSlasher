using System;

/// <summary>
/// Pure static utility — no Unity dependencies, no state.
/// Evaluates whether an emergency bundle offer should be shown and which tier to use.
/// </summary>
public static class FrustrationEvaluator
{
    /// <summary>
    /// Returns true if all conditions are met to show an emergency bundle offer.
    /// </summary>
    public static bool ShouldOffer(LevelFailedContext ctx,
                                   EmergencyBundleConfig config,
                                   GameData data)
    {
        if (config == null || data == null) return false;

        // Daily cap
        if (config.dailyCap > 0)
        {
            long nowDay  = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400;
            long lastDay = data.emergencyBundleLastActivationUtc / 86400;

            int usesToday = (data.emergencyBundleLastActivationUtc == 0 || nowDay != lastDay)
                            ? 0
                            : data.emergencyBundleUsesToday;

            if (usesToday >= config.dailyCap) return false;
        }

        // Cooldown
        if (config.cooldownHours > 0 && data.emergencyBundleLastActivationUtc > 0)
        {
            long elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                           - data.emergencyBundleLastActivationUtc;
            if (elapsed < (long)(config.cooldownHours * 3600)) return false;
        }

        // Loss threshold
        if (ctx.IsBossLevel)
            return data.consecutiveBossLosses >= config.bossFailureThreshold;

        return ctx.ConsecutiveLosses >= config.consecutiveLossThreshold;
    }

    /// <summary>
    /// Selects which bundle tier to offer based on loss count and config rules.
    /// </summary>
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
