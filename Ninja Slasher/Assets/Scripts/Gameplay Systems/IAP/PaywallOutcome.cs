/// <summary>
/// Resultado de un episodio de paywall (Emergency Bundle).
/// Se usa en paywallResolved para segmentar conversión, rebote y expiración.
/// </summary>
public enum PaywallOutcome
{
    Purchased, // El jugador compró el bundle
    Dismissed, // El jugador cerró el overlay manualmente
    Expired    // El countdown llegó a cero sin comprar ni cerrar
}
