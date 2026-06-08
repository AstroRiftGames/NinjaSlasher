using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/DashTurbo")]
public class PowerUpDashTurbo : PowerUpBase
{
    public override void Activate(PowerUpContext context)
    {
        context.DashTurboActive = true;
        context.DashSpeedMultiplier = gameConfig != null ? gameConfig.GetDashTurboSpeedMultiplier() : 1.5f;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.DashTurboActive = false;
        context.DashSpeedMultiplier = 1f;
    }

}
