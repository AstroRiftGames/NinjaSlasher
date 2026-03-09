using UnityEngine;

[CreateAssetMenu(fileName = "EmergencyBundleConfig", menuName = "Store/Emergency Bundle Config")]
public class EmergencyBundleConfig : ScriptableObject
{
    [Header("Emergency Bundle Product IDs")]
    [Tooltip("Primary product ID del bundle Small")]
    public string smallBundleProductId;

    [Tooltip("Primary product ID del bundle Medium")]
    public string mediumBundleProductId;

    [Tooltip("Primary product ID del bundle Large")]
    public string largeBundleProductId;

    [Header("Activation Thresholds")]
    [Tooltip("Derrotas consecutivas en niveles normales para mostrar la oferta.")]
    [Min(1)] public int consecutiveLossThreshold = 3;

    [Tooltip("Derrotas consecutivas en boss levels para mostrar la oferta.")]
    [Min(1)] public int bossFailureThreshold = 2;

    [Header("Tier Selection")]
    [Tooltip("Perdidas mínimas para escalar a LARGE. 0 = nunca.")]
    [Min(0)] public int largeTierLossThreshold = 7;

    [Tooltip("Perdidas mínimas para escalar a MEDIUM. 0 = nunca.")]
    [Min(0)] public int mediumTierLossThreshold = 5;

    [Tooltip("Forzar tier LARGE cuando el jugador está en boss level y se quedó sin vidas.")]
    public bool largeBundleOnBossWithNoLives = true;

    [Header("Daily Limits & Timing")]
    [Tooltip("Máximo de activaciones por día UTC. 0 = sin límite.")]
    [Min(0)] public int dailyCap = 2;

    [Tooltip("Horas de cooldown entre activaciones consecutivas.")]
    [Min(0f)] public float cooldownHours = 4f;

    [Tooltip("Minutos que permanece visible la oferta antes de descartarse automáticamente.")]
    [Min(0.5f)] public float offerDurationMinutes = 5f;

    public string GetProductIdForTier(BundleTier tier) => tier switch
    {
        BundleTier.Large  => largeBundleProductId,
        BundleTier.Medium => mediumBundleProductId,
        _                 => smallBundleProductId,
    };
}
