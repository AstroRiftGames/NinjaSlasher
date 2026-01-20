using UnityEngine;

public class ObjectiveService
{
    private LevelConfiguration config;

    public ObjectiveService(LevelConfiguration levelConfig)
    {
        config = levelConfig;

        if (config == null)
        {
            return;
        }

        if (config.objectives == null || config.objectives.Length == 0)
        {
            return;
        }
    }

    public ObjectiveEvaluationResult Evaluate(LevelStats stats, bool includePreviouslyCompleted = true)
    {
        var result = new ObjectiveEvaluationResult();

        if (config == null || config.objectives == null)
        {
            return result;
        }

        var primaryObjective = config.GetPrimaryObjective();
        if (primaryObjective == null)
        {
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
            bool alreadyCompleted = false;

            if (includePreviouslyCompleted && SaveManager.Instance != null)
            {
                alreadyCompleted = SaveManager.Instance.IsObjectiveCompleted(config.levelId, objective);
            }

            if (alreadyCompleted)
            {
                result.completedObjectives.Add(objective);
                result.starsEarned += objective.starValue;
            }
            else if (objective.CanBeEvaluated(stats, config.levelContext) &&
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