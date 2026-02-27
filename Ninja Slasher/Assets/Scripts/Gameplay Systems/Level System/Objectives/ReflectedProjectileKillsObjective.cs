using UnityEngine;

[CreateAssetMenu(fileName = "ReflectedProjectileKills", menuName = "Game/Objectives/Reflected Projectile Kills")]
public class ReflectedProjectileKillsObjective : ObjectiveData
{
    [Header("REFLECTED KILLS")]
    [Tooltip("Numero de enemigos que deben ser eliminados con proyectiles repelidos")]
    public int requiredKills = 1;

    public override bool IsCompleted(LevelStats stats, LevelContext context)
    {
        return stats.reflectedProjectileKills >= requiredKills;
    }

    public override bool CanBeEvaluated(LevelStats stats, LevelContext context)
    {
        return context.hasParryMechanics && stats.enemiesDefeated >= stats.totalEnemies;
    }

    public override float GetProgress(LevelStats stats, LevelContext context)
    {
        if (requiredKills <= 0) return 1f;
        return Mathf.Clamp01((float)stats.reflectedProjectileKills / requiredKills);
    }
}
