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

        var secondaryObjectives = config.GetSecondaryObjectives();
        foreach (var objective in secondaryObjectives)
        {
            if (objective.CanBeEvaluated(stats, config.levelContext) &&
                objective.IsCompleted(stats, config.levelContext))
            {
                result.starsEarned += objective.starValue;
                result.completedObjectives.Add(objective);
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

            var progress = new SingleObjectiveProgress
            {
                objective = objective,
                progress = objective.GetProgress(stats, config.levelContext),
                isCompleted = objective.IsCompleted(stats, config.levelContext),
                canBeEvaluated = objective.CanBeEvaluated(stats, config.levelContext)
            };

            progressData.objectiveProgresses.Add(progress);
        }

        return progressData;
    }
}