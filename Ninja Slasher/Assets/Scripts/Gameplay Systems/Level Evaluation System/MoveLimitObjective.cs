using UnityEngine;

[CreateAssetMenu(fileName = "MoveLimit", menuName = "Game/Objectives/Move Limit")]
public class MoveLimitObjective : ObjectiveData
{
    [Header("Configuración de Movimientos")]
    public int maxAllowedMoves = 10;

    public override bool IsCompleted(LevelStats stats, LevelContext context)
    {
        return stats.movesUsed <= maxAllowedMoves;
    }

    public override float GetProgress(LevelStats stats, LevelContext context)
    {
        if (stats.movesUsed <= maxAllowedMoves) return 1.0f;
        return Mathf.Clamp01((float)maxAllowedMoves / stats.movesUsed);
    }

    public override bool CanBeEvaluated(LevelStats stats, LevelContext context)
    {
        // Solo evaluar si el nivel principal está completo
        return stats.enemiesDefeated >= stats.totalEnemies;
    }
}