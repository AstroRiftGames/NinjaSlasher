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
    [Header("LEVEL SETTINGS")]
    public LevelObjectives objectives;

    private float elapsedTime;
    private bool isTrackingTime = false;

    public float TimeTaken => elapsedTime;

    void Start()
    {
        elapsedTime = 0f;
        isTrackingTime = true;
    }

    void Update()
    {
        if (isTrackingTime)
        {
            elapsedTime += Time.deltaTime;
        }
    }

    public void StopTimer()
    {
        isTrackingTime = false;
    }

    public int Evaluate(LevelStats stats)
    {
        int stars = 1;

        if (objectives.mustDefeatAllEnemies && stats.enemiesDefeated < stats.totalEnemies)
            return 0;

        if (objectives.enableTimeChallenge && stats.timeTaken <= objectives.maxTimeAllowed)
            stars++;

        if (objectives.enableMoveLimitChallenge && stats.movesUsed <= objectives.maxAllowedMoves)
            stars++;

        if (objectives.enableParryKillChallenge && stats.parryKillDone)
            stars++;

        return Mathf.Min(stars, 3);
    }
}
