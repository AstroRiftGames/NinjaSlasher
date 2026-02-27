using UnityEngine;

[CreateAssetMenu(fileName = "SingleAttackKills", menuName = "Game/Objectives/Single Attack Kills")]
public class SingleAttackKillsObjective : ObjectiveData
{
    [Header("SINGLE ATTACK KILLS")]
    [Tooltip("Numero de enemigos que deben eliminarse en un mismo ataque")]
    public int requiredKills = 2;

    public override bool IsCompleted(LevelStats stats, LevelContext context)
    {
        return stats.maxEnemiesKilledInSingleAttack >= requiredKills;
    }

    public override float GetProgress(LevelStats stats, LevelContext context)
    {
        if (requiredKills <= 0) return 1f;
        return Mathf.Clamp01((float)stats.maxEnemiesKilledInSingleAttack / requiredKills);
    }
}
