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
}
