using UnityEngine;

[CreateAssetMenu(fileName = "PowerUpHawkVision", menuName = "PowerUps/HawkVision")]
public class PowerUpHawkVision : PowerUpBase
{
    public float maxDistance = 50f;

    public override void Activate(PowerUpContext context)
    {
        context.TrajectoryGuideActive = true;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.TrajectoryGuideActive = false;
    }
}