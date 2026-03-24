/// <summary>
/// Resultado de un episodio de life wall (sin vidas disponibles).
/// Se usa en el evento lifeWallResolved para distinguir cómo el jugador
/// salió del bloqueo, permitiendo segmentar retención vs monetización.
/// </summary>
public enum LifeWallOutcome
{
    AdReward,    // Resolvió viendo un rewarded ad
    IapPurchase, // Resolvió comprando vidas via IAP o bundle
    Waited,      // Resolvió esperando la regeneración natural
    Abandoned    // Cerró el overlay sin resolver (abandono de sesión)
}
