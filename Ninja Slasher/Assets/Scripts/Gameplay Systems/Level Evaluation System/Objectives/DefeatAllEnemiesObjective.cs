using UnityEngine;

[CreateAssetMenu(fileName = "DefeatAllEnemies", menuName = "Game/Objectives/Defeat All Enemies")]
public class DefeatAllEnemiesObjective : ObjectiveData
{
    public override bool IsCompleted(LevelStats stats, LevelContext context)
    {
        return stats.enemiesDefeated >= stats.totalEnemies;
    }

    public override float GetProgress(LevelStats stats, LevelContext context)
    {
        if (stats.totalEnemies == 0) return 1.0f;
        return Mathf.Clamp01((float)stats.enemiesDefeated / stats.totalEnemies);
    }
}
