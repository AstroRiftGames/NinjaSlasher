using UnityEngine;

public class ObjectiveEvaluator
{
    private LevelConfiguration config;

    public ObjectiveEvaluator(LevelConfiguration levelConfig)
    {
        config = levelConfig;
    }

    public ObjectiveEvaluationResult Evaluate(LevelStats stats)
    {
        var result = new ObjectiveEvaluationResult();

        if (config == null || config.objectives == null)
        {
            Debug.LogError("No hay configuración de objetivos válida");
            return result;
        }

        var primaryObjective = config.GetPrimaryObjective();
        if (primaryObjective == null)
        {
            Debug.LogError("No se encontró objetivo principal");
            return result;
        }

        bool primaryCompleted = primaryObjective.IsCompleted(stats, config.levelContext);
        if (!primaryCompleted)
        {
            result.starsEarned = 0;
            result.primaryCompleted = false;
            return result;
        }

        result.primaryCompleted = true;
        result.starsEarned = primaryObjective.starValue;
        result.completedObjectives.Add(primaryObjective);

        var secondaryObjectives = config.GetSecondaryObjectives();
        foreach (var objective in secondaryObjectives)
        {
            bool alreadyCompleted = SaveManager.Instance?.IsObjectiveCompleted(config.levelId, objective) ?? false;

            if (alreadyCompleted)
            {
                result.completedObjectives.Add(objective);
                result.starsEarned += objective.starValue;
                Debug.Log($"[ObjectiveEvaluator] Objetivo '{objective.objectiveName}' ya completado previamente");
            }
            else if (objective.CanBeEvaluated(stats, config.levelContext) &&
                     objective.IsCompleted(stats, config.levelContext))
            {
                result.starsEarned += objective.starValue;
                result.completedObjectives.Add(objective);
                Debug.Log($"[ObjectiveEvaluator] ¡Nuevo objetivo completado! '{objective.objectiveName}'");
            }
        }

        result.starsEarned = Mathf.Min(result.starsEarned, 3);
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
}