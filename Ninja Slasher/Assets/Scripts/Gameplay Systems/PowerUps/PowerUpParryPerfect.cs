using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/ParryPerfect")]
public class PowerUpParryPerfect : PowerUpBase
{
    public override void Activate(PowerUpContext context)
    {
        context.ParryPerfectActive = true;
        float bonus = gameConfig != null ? gameConfig.GetParryPerfectBonusWindow() : 0.3f;
        context.ParryBonusWindow += bonus;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.ParryPerfectActive = false;
        float bonus = gameConfig != null ? gameConfig.GetParryPerfectBonusWindow() : 0.3f;
        context.ParryBonusWindow -= bonus;
    }

}
