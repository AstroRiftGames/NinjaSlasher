using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/SecondChance")]
public class PowerUpSecondChance : PowerUpBase
{
    public override void Activate(PowerUpContext context)
    {
        context.SecondChanceActive = true;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.SecondChanceActive = false;
    }

    public override void OnUseConsumed(PowerUpContext context)
    {
    }
}
