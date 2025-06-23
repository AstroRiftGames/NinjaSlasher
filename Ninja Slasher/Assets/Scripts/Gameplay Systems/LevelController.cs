using UnityEngine;
using System;

[Serializable]
public class LevelObjectives
{
    public bool mustDefeatAllEnemies = true;

    [Header("TIME CHALLENGE")]
    public bool enableTimeChallenge;
    public float maxTimeAllowed;

    [Header("MOVEMENT CHALLENGE")]
    public bool enableMoveLimitChallenge;
    public int maxAllowedMoves;

    [Header("PARRY CHALLENGE")]
    public bool enableParryKillChallenge;
}

public class LevelStats
{
    public float timeTaken;
    public int enemiesDefeated;
    public int totalEnemies;

    public int movesUsed;
    public bool parryKillDone;
}

public class LevelController : MonoBehaviour
{
    [Header("LEVEL DURATION")]
    [Tooltip("Tiempo base que tiene el jugador para completar el nivel")]
    [SerializeField] private float levelDuration;

    [Header("LEVEL OBJECTIVES")]
    [SerializeField] private LevelObjectives objectives;

    private float elapsedTime;
    private float timeRemaining;
    private bool isTrackingTime;

    public float TimeTaken => elapsedTime;

    public event Action<float> OnTimeChanged;
    public event Action OnTimeExpired;

    void Start()
    {
        elapsedTime = 0f;
        timeRemaining = levelDuration;
        isTrackingTime = true;
        OnTimeChanged?.Invoke(timeRemaining);
    }

    void Update()
    {
        if (!isTrackingTime)
            return;

        elapsedTime += Time.deltaTime;
        timeRemaining -= Time.deltaTime;
        OnTimeChanged?.Invoke(timeRemaining);

        if (timeRemaining <= 0f)
        {
            isTrackingTime = false;
            Debug.Log("Level time expired.");
            OnTimeExpired?.Invoke();
        }
    }

    public void AddTime(float bonus)
    {
        timeRemaining += bonus;
        Debug.Log($"Added bonus time: {bonus:F2}s. New Time Remaining: {timeRemaining:F2}s");
        OnTimeChanged?.Invoke(timeRemaining);
    }

    public void StopTimer()
    {
        isTrackingTime = false;
    }

    public int Evaluate(LevelStats stats)
    {
        int stars = 1;

        // Fallo si no derrota a todos los enemigos
        if (objectives.mustDefeatAllEnemies && stats.enemiesDefeated < stats.totalEnemies)
            return 0;

        // Estrella por desafío de tiempo extra
        if (objectives.enableTimeChallenge && stats.timeTaken <= objectives.maxTimeAllowed)
            stars++;

        // Estrella por limite de movimientos
        if (objectives.enableMoveLimitChallenge && stats.movesUsed <= objectives.maxAllowedMoves)
            stars++;

        // Estrella por parry kill
        if (objectives.enableParryKillChallenge && stats.parryKillDone)
            stars++;

        return Mathf.Min(stars, 3);
    }
}
