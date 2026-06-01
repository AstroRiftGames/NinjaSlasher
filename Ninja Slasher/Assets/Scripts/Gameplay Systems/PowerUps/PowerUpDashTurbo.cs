using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/DashTurbo")]
public class PowerUpDashTurbo : PowerUpBase
{
    public override void Activate(PowerUpContext context)
    {
        context.DashTurboActive = true;
        context.DashCooldownMultiplier = gameConfig != null ? gameConfig.GetDashTurboCooldownMultiplier() : 0.25f;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.DashTurboActive = false;
        context.DashCooldownMultiplier = 1f;
    }

}
