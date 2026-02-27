using UnityEngine;

[CreateAssetMenu(fileName = "BL4ZTExplosionKills", menuName = "Game/Objectives/BL4ZT Explosion Kills")]
public class BL4ZTExplosionKillsObjective : ObjectiveData
{
    [Header("BL4ZT EXPLOSION KILLS")]
    [Tooltip("Numero total de enemigos que deben ser eliminados por explosiones de BL4-ZT")]
    public int requiredKills = 1;

    public override bool IsCompleted(LevelStats stats, LevelContext context)
    {
        return stats.bl4ztExplosionKills >= requiredKills;
    }

    public override float GetProgress(LevelStats stats, LevelContext context)
    {
        if (requiredKills <= 0) return 1f;
        return Mathf.Clamp01((float)stats.bl4ztExplosionKills / requiredKills);
    }
}
