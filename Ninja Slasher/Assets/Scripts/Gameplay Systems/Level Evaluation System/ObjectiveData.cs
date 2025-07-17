using System;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelObjectives", menuName = "Game/Level Objectives Data")]
public abstract class ObjectiveData : ScriptableObject
{
    [Header("GENERAL SETTINGS")]
    public string objectiveName;
    public string description;
    public bool isSecondary = true;
    public int starValue = 1;

    public abstract bool IsCompleted(LevelStats stats, LevelContext context);

    public virtual bool CanBeEvaluated(LevelStats stats, LevelContext context)
    {
        return true;
    }

    public virtual float GetProgress(LevelStats stats, LevelContext context)
    {
        return IsCompleted(stats, context) ? 1.0f : 0.0f;
    }
}

[Serializable]
public class LevelContext
{
    public float levelDuration;
    public int totalEnemiesInLevel;
    public bool hasParryMechanics;
    public bool hasMovementRestrictions;
}