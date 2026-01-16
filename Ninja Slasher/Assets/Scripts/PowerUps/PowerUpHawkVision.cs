using UnityEngine;

[CreateAssetMenu(fileName = "PowerUpHawkVision", menuName = "PowerUps/HawkVision")]
public class PowerUpHawkVision : PowerUpBase
{
    public float maxDistance = 50f;

    public override void Activate(PowerUpContext context)
    {
        context.TrajectoryGuideActive = true;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.TrajectoryGuideActive = false;
    }

    public override void OnUseConsumed(PowerUpContext context)
    {
        Debug.Log($"[HawkVision] Uso consumido. Restantes: {context.DashTurboUsesRemaining}");

        if (context.HawkVisionUsesRemaining == 1)
        {
            Debug.LogWarning("[HawkVision] Ultimo uso disponible");
        }
    }
}