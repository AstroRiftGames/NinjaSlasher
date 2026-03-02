using UnityEngine;

[CreateAssetMenu(
    fileName = "EmergencyBundleConfig",
    menuName = "NinjaSlasher/Emergency Bundles/Config",
    order = 0)]
public class EmergencyBundleConfig : ScriptableObject
{
    // ACTIVATION THRESHOLDS
    // Cuántas derrotas se necesitan para que el sistema evalúe mostrar la oferta

    [Header("Activation Thresholds")]

    [Tooltip("Número de derrotas consecutivas en niveles normales que activan la evaluación " +
             "del Emergency Bundle. Debe ser >= 1.")]
    [Min(1)]
    public int consecutiveLossThreshold = 3;

    [Tooltip("Número de derrotas consecutivas en niveles boss que activan la evaluación. " +
             "Umbral independiente porque los boss levels tienen mayor frustración percibida.")]
    [Min(1)]
    public int bossFailureThreshold = 2;

    // DAILY LIMITS & TIMING
    // Controlan con qué frecuencia puede aparecer la oferta para evitar fatiga.

    [Header("Daily Limits & Timing")]

    [Tooltip("Número máximo de veces que se puede mostrar (y activar) el Emergency Bundle " +
             "en un mismo día UTC. 0 = sin límite (no recomendado en producción).")]
    [Min(0)]
    public int dailyCap = 2;

    [Tooltip("Horas de cooldown entre activaciones consecutivas del Emergency Bundle. " +
             "Evita mostrar la oferta repetidamente si el jugador rechazó la anterior.")]
    [Min(0f)]
    public float cooldownHours = 4f;

    [Tooltip("Minutos que permanece visible la oferta antes de descartarse automáticamente. " +
             "El timer lo gestiona EmergencyBundleService; este campo solo define la duración.")]
    [Min(0.5f)]
    public float offerDurationMinutes = 5f;

    // IAP PRODUCT IDs
    // IDs de producto registrados en Unity IAP / App Store / Google Play.
    // Deben coincidir exactamente con los IDs en IAPManager.

    [Header("IAP Product IDs")]

    [Tooltip("Product ID del bundle pequeño (p. ej. pack de vidas básico). " +
             "Debe estar registrado en IAPManager y en el portal de la tienda.")]
    public string smallBundleProductId = "";

    [Tooltip("Product ID del bundle mediano (p. ej. pack de vidas + power-up). " +
             "Debe estar registrado en IAPManager y en el portal de la tienda.")]
    public string mediumBundleProductId = "";

    [Tooltip("Product ID del bundle grande (p. ej. pack premium con todas las ventajas). " +
             "Debe estar registrado en IAPManager y en el portal de la tienda.")]
    public string largeBundleProductId = "";
}
