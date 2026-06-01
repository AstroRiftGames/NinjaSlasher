using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/ExtraTime")]
public class PowerUpExtraTime : PowerUpBase
{
    public override void Activate(PowerUpContext context)
    {
        context.ExtraTimeActive = true;
        context.ExtraTimePercent = gameConfig != null ? gameConfig.GetExtraTimeBonus() : 0.5f;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.ExtraTimeActive = false;
        context.ExtraTimePercent = 0f;
    }

}
