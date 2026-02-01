using System;
using UnityEngine;

[Serializable]
public class LevelUnlockRequirements
{
    [Header("UNLOCK REQUIREMENTS")]
    [Tooltip("Solo nivel 1 debería tener esto en true")]
    public bool isInitiallyUnlocked = false;
    [Tooltip("Estrellas requeridas para acceder a este nivel (solo para jefes)")]
    public int minimumStarsRequired = 0;
    [Tooltip("Solo niveles jefe (10, 20, 30, etc.) deben tener esto en true")]
    public bool isBossLevel = false;
    [Tooltip("ID del área al que pertenece este nivel (1-5)")]
    public int areaId = 1;
}

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
    [Tooltip("Características específicas del nivel para evaluación de objetivos")]
    public LevelContext levelContext;

    [Header("UNLOCK REQUIREMENTS")]
    public LevelUnlockRequirements unlockRequirements;
    
    [Header("MUSIC")]
    public AudioEvent gameplayMusic;
    public AudioEvent bossMusic;

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
            Debug.LogWarning($"[{name}] No hay objetivos principales definidos");
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