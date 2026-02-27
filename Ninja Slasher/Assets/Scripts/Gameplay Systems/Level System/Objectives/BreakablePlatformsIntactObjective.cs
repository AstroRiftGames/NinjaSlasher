using UnityEngine;

[CreateAssetMenu(fileName = "BreakablePlatformsIntact", menuName = "Game/Objectives/Breakable Platforms Intact")]
public class BreakablePlatformsIntactObjective : ObjectiveData
{
    public override bool IsCompleted(LevelStats stats, LevelContext context)
    {
        return stats.platformsBroken == 0;
    }

    public override bool CanBeEvaluated(LevelStats stats, LevelContext context)
    {
        return stats.totalBreakablePlatforms > 0;
    }

    public override float GetProgress(LevelStats stats, LevelContext context)
    {
        return stats.platformsBroken == 0 ? 1f : 0f;
    }
}
