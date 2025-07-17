using UnityEngine;

[CreateAssetMenu(fileName = "ParryKill", menuName = "Game/Objectives/Parry Kill")]
public class ParryKillObjective : ObjectiveData
{
    public override bool IsCompleted(LevelStats stats, LevelContext context)
    {
        return stats.parryKillDone;
    }

    public override bool CanBeEvaluated(LevelStats stats, LevelContext context)
    {
        return context.hasParryMechanics && stats.enemiesDefeated >= stats.totalEnemies;
    }
}