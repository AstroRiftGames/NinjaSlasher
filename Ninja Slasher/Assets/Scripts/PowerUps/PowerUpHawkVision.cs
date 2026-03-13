using UnityEngine;

[CreateAssetMenu(fileName = "PowerUpHawkVision", menuName = "PowerUps/HawkVision")]
public class PowerUpHawkVision : PowerUpBase
{
    public float maxDistance = 50f;

    public override void Activate(PowerUpContext context)
    {
        context.HawkVisionActive = true;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.HawkVisionActive = false;
    }

    public override void OnUseConsumed(PowerUpContext context)
    {
        Debug.Log($"[HawkVision] Uso consumido. Restantes: {context.HawkVisionUsesRemaining}");

        if (context.HawkVisionUsesRemaining == 1)
        {
            Debug.LogWarning("[HawkVision] Ultimo uso disponible");
        }
    }
}