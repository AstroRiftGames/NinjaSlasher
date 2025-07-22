using UnityEngine;

[CreateAssetMenu(fileName = "TimeChallenge", menuName = "Game/Objectives/Time Challenge")]
public class TimeChallengeObjective : ObjectiveData
{
    [Header("TIME LIMIT")]
    public float maxTimeAllowed = 30f;

    public override bool IsCompleted(LevelStats stats, LevelContext context)
    {
        return stats.timeTaken <= maxTimeAllowed;
    }

    public override float GetProgress(LevelStats stats, LevelContext context)
    {
        if (stats.timeTaken <= maxTimeAllowed) return 1.0f;
        return Mathf.Clamp01(maxTimeAllowed / stats.timeTaken);
    }
}