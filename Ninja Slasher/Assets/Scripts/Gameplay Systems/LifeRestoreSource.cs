/// <summary>
/// Fuente de restauración de una vida, usada como parámetro en
/// LifeManager.AddLife() y RecordLifeRestored().
///
/// Reemplaza el string "manualAdd" que agrupaba incorrectamente ad reward,
/// IAP, daily bonus y otras fuentes en una sola categoría opaca.
/// </summary>
public enum LifeRestoreSource
{
    Unknown,          // Fuente no especificada (legacy o no identificada)
    TimeRegeneration, // Regeneración automática por tiempo
    AdReward,         // Ad recompensado (rewarded ad)
    IapPurchase,      // Compra IAP
    DailyBonus,       // Recompensa diaria
    DailyWheel,       // Ruleta diaria
    EmergencyBundle,  // Bundle de emergencia
    Debug             // Solo para testing en editor
}
