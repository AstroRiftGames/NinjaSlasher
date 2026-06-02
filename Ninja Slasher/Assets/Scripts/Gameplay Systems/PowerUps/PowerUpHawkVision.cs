using UnityEngine;

[CreateAssetMenu(fileName = "PowerUpHawkVision", menuName = "PowerUps/HawkVision")]
public class PowerUpHawkVision : PowerUpBase
{
    public override void Activate(PowerUpContext context)
    {
        context.HawkVisionActive = true;
        context.HawkVisionInitialTimeScale = gameConfig != null
            ? gameConfig.GetHawkVisionInitialTimeScale()
            : 0.35f;
        context.HawkVisionInitialSlowDuration = gameConfig != null
            ? gameConfig.GetHawkVisionInitialSlowDuration()
            : 2f;
        context.HawkVisionTrajectoryMaxDistance = gameConfig != null
            ? gameConfig.GetHawkVisionTrajectoryMaxDistance()
            : 100f;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.HawkVisionActive = false;
        context.HawkVisionInitialTimeScale = 1f;
        context.HawkVisionInitialSlowDuration = 0f;
        context.HawkVisionTrajectoryMaxDistance = 100f;
    }

}
