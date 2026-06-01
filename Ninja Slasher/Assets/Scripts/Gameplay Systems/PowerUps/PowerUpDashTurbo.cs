using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/DashTurbo")]
public class PowerUpDashTurbo : PowerUpBase
{
    [Range(0.1f, 1f)] public float dashCooldownMultiplier = 0.7f;

    public override void Activate(PowerUpContext context)
    {
        context.DashTurboActive = true;
        context.DashCooldownMultiplier = dashCooldownMultiplier;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.DashTurboActive = false;
        context.DashCooldownMultiplier = 1f;
    }

    public override void OnUseConsumed(PowerUpContext context)
    {
    }
}
