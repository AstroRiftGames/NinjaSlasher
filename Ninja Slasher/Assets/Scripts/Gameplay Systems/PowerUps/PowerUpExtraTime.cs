using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/ExtraTime")]
public class PowerUpExtraTime : PowerUpBase
{
    [Range(0f, 2f)] public float extraPercent;

    public override void Activate(PowerUpContext context)
    {
        context.ExtraTimeActive = true;
        context.ExtraTimePercent = extraPercent;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.ExtraTimeActive = false;
        context.ExtraTimePercent = 0f;
    }

    public override void OnUseConsumed(PowerUpContext context)
    {
    }
}
