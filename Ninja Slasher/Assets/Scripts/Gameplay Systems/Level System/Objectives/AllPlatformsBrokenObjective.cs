using UnityEngine;

[CreateAssetMenu(fileName = "AllPlatformsBroken", menuName = "Game/Objectives/All Platforms Broken")]
public class AllPlatformsBrokenObjective : ObjectiveData
{
    public override bool IsCompleted(LevelStats stats, LevelContext context)
    {
        return stats.totalBreakablePlatforms > 0
            && stats.platformsBroken >= stats.totalBreakablePlatforms;
    }

    public override bool CanBeEvaluated(LevelStats stats, LevelContext context)
    {
        return stats.totalBreakablePlatforms > 0;
    }

    public override float GetProgress(LevelStats stats, LevelContext context)
    {
        if (stats.totalBreakablePlatforms <= 0) return 0f;
        return Mathf.Clamp01((float)stats.platformsBroken / stats.totalBreakablePlatforms);
    }
}
