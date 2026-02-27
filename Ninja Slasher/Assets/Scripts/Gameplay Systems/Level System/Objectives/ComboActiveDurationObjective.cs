using UnityEngine;

[CreateAssetMenu(fileName = "ComboActiveDuration", menuName = "Game/Objectives/Combo Active Duration")]
public class ComboActiveDurationObjective : ObjectiveData
{
    [Header("COMBO DURATION")]
    [Tooltip("Duración minima en segundos que el combo debe mantenerse activo de forma continua")]
    public float requiredSeconds = 5f;

    public override bool IsCompleted(LevelStats stats, LevelContext context)
    {
        return stats.maxComboActiveDuration >= requiredSeconds;
    }

    public override float GetProgress(LevelStats stats, LevelContext context)
    {
        if (requiredSeconds <= 0f) return 1f;
        return Mathf.Clamp01(stats.maxComboActiveDuration / requiredSeconds);
    }
}
