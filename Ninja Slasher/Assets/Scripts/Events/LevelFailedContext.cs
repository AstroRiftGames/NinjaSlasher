using System;

[Serializable]
public struct LevelFailedContext
{
    public string Reason;

    public int LevelId;

    public bool IsBossLevel;

    public int ConsecutiveLosses;

    public int LivesRemaining;
}
