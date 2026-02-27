using UnityEngine;

[CreateAssetMenu(fileName = "MaxComboLevel", menuName = "Game/Objectives/Max Combo Level")]
public class MaxComboLevelObjective : ObjectiveData
{
    [Header("COMBO LEVEL")]
    [Tooltip("Nivel de combo maximo que debe alcanzarse al menos una vez durante el nivel")]
    public int requiredComboLevel = 3;

    public override bool IsCompleted(LevelStats stats, LevelContext context)
    {
        return stats.maxComboLevelReached >= requiredComboLevel;
    }

    public override float GetProgress(LevelStats stats, LevelContext context)
    {
        if (requiredComboLevel <= 0) return 1f;
        return Mathf.Clamp01((float)stats.maxComboLevelReached / requiredComboLevel);
    }
}
