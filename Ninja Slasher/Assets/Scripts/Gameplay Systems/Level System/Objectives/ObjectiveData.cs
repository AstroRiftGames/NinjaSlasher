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

    [Header("STABLE ID")]
    [SerializeField] private string _stableId;
    public string StableId => _stableId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(_stableId))
        {
            GenerateNewId();
        }
    }

    private void GenerateNewId()
    {
        _stableId = System.Guid.NewGuid().ToString();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

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
    [Header("LEVEL CONFIGURATION")]
    [Tooltip("Se calcula autom�ticamente al inicio del nivel")]
    public int totalEnemiesInLevel;
    [Header("LEVEL MECHANICS")]
    [Tooltip("Indica si el nivel incluye enemigos que disparan proyectiles")]
    public bool hasParryMechanics;
    [Tooltip("Indica si el nivel tiene restricciones especiales de movimiento")]
    public bool hasMovementRestrictions;

    public void Initialize(int enemyCount)
    {
        totalEnemiesInLevel = enemyCount;
    }
}