using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/ParryPerfect")]
public class PowerUpParryPerfect : PowerUpBase
{
    public float extraParryWindow = 0.3f;

    public override void Activate(PowerUpContext context)
    {
        context.ParryPerfectActive = true;
        context.ParryBonusWindow += extraParryWindow;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.ParryPerfectActive = false;
        context.ParryBonusWindow -= extraParryWindow;
    }

    public override void OnUseConsumed(PowerUpContext context)
    {
        Debug.Log($"[ParryPerfect] Uso consumido. Restantes: {context.DashTurboUsesRemaining}");

        if (context.ParryPerfectUsesRemaining == 1)
        {
            Debug.LogWarning("[ParryPerfect] Ultimo uso disponible");
        }
    }
}
