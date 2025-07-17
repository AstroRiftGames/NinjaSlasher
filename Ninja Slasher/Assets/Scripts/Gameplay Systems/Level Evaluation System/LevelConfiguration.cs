using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfiguration", menuName = "Game/Level Configuration")]
public class LevelConfiguration : ScriptableObject
{
    [Header("LEVEL INFORMATION")]
    public int levelId;
    public string levelName;
    public float levelDuration;

    [Header("OBJECTIVES")]
    public ObjectiveData[] objectives;

    [Header("LEVEL CONTEXT")]
    public LevelContext levelContext;

    private void OnValidate()
    {
        bool hasPrimary = false;
        foreach (var objective in objectives)
        {
            if (objective != null && !objective.isSecondary)
            {
                hasPrimary = true;
                break;
            }
        }

        if (!hasPrimary && objectives.Length > 0)
        {
            Debug.LogWarning($"[{name}] No hay objetivos principales definidos. Se recomienda tener al menos uno.");
        }
    }

    public ObjectiveData GetPrimaryObjective()
    {
        foreach (var objective in objectives)
        {
            if (objective != null && !objective.isSecondary)
                return objective;
        }
        return null;
    }

    public ObjectiveData[] GetSecondaryObjectives()
    {
        var secondaryList = new System.Collections.Generic.List<ObjectiveData>();
        foreach (var objective in objectives)
        {
            if (objective != null && objective.isSecondary)
                secondaryList.Add(objective);
        }
        return secondaryList.ToArray();
    }

    public int GetMaxPossibleStars()
    {
        int maxStars = 0;
        foreach (var objective in objectives)
        {
            if (objective != null)
                maxStars += objective.starValue;
        }
        return Mathf.Min(maxStars, 3);
    }
}