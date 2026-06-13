using UnityEngine;

[CreateAssetMenu(fileName = "PowerUpEnhancedParry", menuName = "PowerUps/EnhancedParry")]
public class PowerUpEnhancedParry : PowerUpBase
{
    public override void Activate(PowerUpContext context)
    {
        context.EnhancedParryActive = true;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.EnhancedParryActive = false;
    }
}
