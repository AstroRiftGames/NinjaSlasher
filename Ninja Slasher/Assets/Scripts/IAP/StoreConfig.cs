using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PowerUpRewardEntry
{
    public PowerUpType type;
    [Min(1)] public int quantity;
}

[Serializable]
public class BundleRewardData
{
    [Header("Lives")]
    [Tooltip("Si true, se activan vidas ilimitadas por unlimitedLivesDurationMinutes. " +
             "Si false, se suman regularLivesCount vidas normales.")]
    public bool unlimitedLives;

    [Tooltip("Duración en minutos de las vidas ilimitadas (solo si unlimitedLives = true).")]
    [Min(0.5f)] public float unlimitedLivesDurationMinutes = 5f;

    [Tooltip("Vidas regulares a añadir (solo si unlimitedLives = false).")]
    [Min(0)] public int regularLivesCount;

    [Header("Power-ups")]
    [Tooltip("Lista de power-ups que se añaden al inventario del jugador.")]
    public List<PowerUpRewardEntry> powerUps = new();

    [Header("Currency")]
    [Tooltip("Monedas a otorgar.")]
    [Min(0)] public int coins;
}

[Serializable]
public class BundleDisplayData
{
    [Tooltip("Nombre visible del bundle en el overlay (ej. 'Pack Rescate', 'Pack Élite').")]
    public string bundleName;

    [Tooltip("Ícono del bundle que se muestra en EmergencyBundleOverlay.")]
    public Sprite icon;
}

[Serializable]
public class BundleDefinition
{
    [Tooltip("All IAP product IDs that resolve to this bundle.\n" +
             "Index 0 = primary (used by the emergency overlay).\n" +
             "Additional entries = store or alternative SKUs.")]
    public string[] productIds = new string[0];

    public BundleDisplayData display;
    public BundleRewardData  reward;

    public string PrimaryProductId
        => productIds != null && productIds.Length > 0 ? productIds[0] : string.Empty;

    public bool MatchesProductId(string id)
        => productIds != null && Array.Exists(productIds, pid => pid == id);
}

public enum BundleTier { Small, Medium, Large }

[CreateAssetMenu(
    fileName = "EmergencyBundleConfig",
    menuName = "Bundles/Emergency Bundles/Config",
    order = 0)]
public class StoreConfig : ScriptableObject
{
    [Header("Activation Thresholds")]

    [Tooltip("Derrotas consecutivas en niveles normales para activar la evaluación.")]
    [Min(1)] public int consecutiveLossThreshold = 3;

    [Tooltip("Derrotas consecutivas en boss levels para activar la evaluación.")]
    [Min(1)] public int bossFailureThreshold = 2;

    [Header("Tier Selection")]

    [Tooltip("Pérdidas mínimas para mostrar LARGE. 0 = nunca escalar a Large por contador.")]
    [Min(0)] public int largeTierLossThreshold = 7;

    [Tooltip("Pérdidas mínimas para mostrar MEDIUM. 0 = nunca escalar a Medium por contador.")]
    [Min(0)] public int mediumTierLossThreshold = 5;

    [Tooltip("Forzar LARGE cuando el jugador está en boss level Y se quedó sin vidas.")]
    public bool largeBundleOnBossWithNoLives = true;

    [Header("Daily Limits & Timing")]

    [Tooltip("Máximo de activaciones por día UTC. 0 = sin límite.")]
    [Min(0)] public int dailyCap = 2;

    [Tooltip("Horas de cooldown entre activaciones consecutivas.")]
    [Min(0f)] public float cooldownHours = 4f;

    [Tooltip("Minutos que permanece visible la oferta antes de descartarse.")]
    [Min(0.5f)] public float offerDurationMinutes = 5f;

    [Header("Bundle Definitions")]
    public BundleDefinition smallBundle;
    public BundleDefinition mediumBundle;
    public BundleDefinition largeBundle;

    public string GetProductId(BundleTier tier) => tier switch
    {
        BundleTier.Medium => mediumBundle.PrimaryProductId,
        BundleTier.Large  => largeBundle.PrimaryProductId,
        _                 => smallBundle.PrimaryProductId,
    };

    public BundleRewardData GetReward(BundleTier tier) => tier switch
    {
        BundleTier.Medium => mediumBundle.reward,
        BundleTier.Large  => largeBundle.reward,
        _                 => smallBundle.reward,
    };

    public BundleDisplayData GetDisplay(BundleTier tier) => tier switch
    {
        BundleTier.Medium => mediumBundle.display,
        BundleTier.Large  => largeBundle.display,
        _                 => smallBundle.display,
    };

    public BundleRewardData GetRewardByProductId(string productId)
    {
        if (smallBundle.MatchesProductId(productId))  return smallBundle.reward;
        if (mediumBundle.MatchesProductId(productId)) return mediumBundle.reward;
        if (largeBundle.MatchesProductId(productId))  return largeBundle.reward;
        return null;
    }
}
