using UnityEngine;

public class ObjectiveService
{
    private LevelConfiguration config;

    public ObjectiveService(LevelConfiguration levelConfig)
    {
        config = levelConfig;

        Debug.Log($"[ObjectiveService] Servicio creado con {config.objectives.Length} objetivos");
    }

    public ObjectiveEvaluationResult Evaluate(LevelStats stats, bool includePreviouslyCompleted = true)
    {
        var result = new ObjectiveEvaluationResult();

        if (config == null || config.objectives == null)
        {
            Debug.LogError("[ObjectiveService] No hay configuración de objetivos válida");
            return result;
        }

        var primaryObjective = config.GetPrimaryObjective();
        if (primaryObjective == null)
        {
            Debug.LogError("[ObjectiveService] No se encontró objetivo principal");
            return result;
        }

        bool primaryCompleted = primaryObjective.IsCompleted(stats, config.levelContext);
        if (!primaryCompleted)
        {
            result.starsEarned = 0;
            result.primaryCompleted = false;
            Debug.Log($"[ObjectiveService] Objetivo principal NO completado");
            return result;
        }

        result.primaryCompleted = true;
        result.starsEarned = primaryObjective.starValue;
        result.completedObjectives.Add(primaryObjective);

        Debug.Log($"[ObjectiveService] Objetivo principal completado: {primaryObjective.objectiveName}");

        var secondaryObjectives = config.GetSecondaryObjectives();
        foreach (var objective in secondaryObjectives)
        {
            bool alreadyCompleted = false;

            if (includePreviouslyCompleted && SaveManager.Instance != null)
            {
                alreadyCompleted = SaveManager.Instance.IsObjectiveCompleted(config.levelId, objective);
            }

            if (alreadyCompleted)
            {
                result.completedObjectives.Add(objective);
                result.starsEarned += objective.starValue;
                Debug.Log($"[ObjectiveService] Objetivo '{objective.objectiveName}' completado previamente");
            }
            else if (objective.CanBeEvaluated(stats, config.levelContext) &&
                     objective.IsCompleted(stats, config.levelContext))
            {
                result.starsEarned += objective.starValue;
                result.completedObjectives.Add(objective);
                Debug.Log($"[ObjectiveService] ¡Nuevo objetivo completado! '{objective.objectiveName}'");
            }
        }

        result.starsEarned = Mathf.Min(result.starsEarned, 3);

        Debug.Log($"[ObjectiveService] Evaluación completa - {result.starsEarned} estrellas");
        return result;
    }

    public ObjectiveProgressData GetProgressData(LevelStats stats)
    {
        var progressData = new ObjectiveProgressData();

        if (config?.objectives == null) return progressData;

        foreach (var objective in config.objectives)
        {
            if (objective == null) continue;

            bool wasCompleted = SaveManager.Instance?.IsObjectiveCompleted(config.levelId, objective) ?? false;

            var progress = new SingleObjectiveProgress
            {
                objective = objective,
                progress = wasCompleted ? 1.0f : objective.GetProgress(stats, config.levelContext),
                isCompleted = wasCompleted || objective.IsCompleted(stats, config.levelContext),
                canBeEvaluated = objective.CanBeEvaluated(stats, config.levelContext)
            };

            progressData.objectiveProgresses.Add(progress);
        }

        return progressData;
    }

    public bool CanEvaluate(LevelStats stats)
    {
        var primaryObjective = config.GetPrimaryObjective();
        if (primaryObjective == null) return false;

        return primaryObjective.IsCompleted(stats, config.levelContext);
    }
}