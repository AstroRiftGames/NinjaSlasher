using UnityEngine;

[CreateAssetMenu(fileName = "PowerUpHawkVision", menuName = "PowerUps/HawkVision")]
public class PowerUpHawkVision : PowerUpBase
{
    public Color lineColor = new Color(0f, 1f, 1f, 0.7f);

    public float lineWidth = 0.1f;

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