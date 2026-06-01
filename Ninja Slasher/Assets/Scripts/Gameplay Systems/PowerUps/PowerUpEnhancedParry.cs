using UnityEngine;

[CreateAssetMenu(fileName = "PowerUpEnhancedParry", menuName = "PowerUps/EnhancedParry")]
public class PowerUpEnhancedParry : PowerUpBase
{
    public override void Activate(PowerUpContext context)
    {
        context.EnhancedParryActive = true;
        context.EnhancedParryBounces = gameConfig != null ? gameConfig.GetEnhancedParryBounces() : 2;
        context.EnhancedParryVelocityRetention = gameConfig != null ? gameConfig.GetEnhancedParryVelocityRetention() : 0.9f;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.EnhancedParryActive = false;
        context.EnhancedParryBounces = 0;
        context.EnhancedParryVelocityRetention = 1f;
    }

}
