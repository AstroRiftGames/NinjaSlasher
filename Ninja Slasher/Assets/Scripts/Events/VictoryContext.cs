using System;
using System.Collections.Generic;

public enum VictoryModalVariant
{
    Normal,
    BossClear
}

[Serializable]
public class VictoryReward
{
    public string displayName;
    public int quantity;
}

[Serializable]
public class VictoryContext
{
    public VictoryModalVariant Variant = VictoryModalVariant.Normal;
    public bool IsBossLevel;
    public bool UnlockedNewArea;
    public int UnlockedAreaId;
    public string UnlockedAreaName;
    public string BossVictoryMessage;
    public string BossUnlockSubtitle;
    public string BossUnlockDetail;
    public int StarsEarned;
    public List<VictoryReward> Rewards = new List<VictoryReward>();
}

[Serializable]
public class ProgressionUnlockResult
{
    public bool CompletedBossLevel;
    public bool UnlockedNewArea;
    public int UnlockedAreaId;
    public string UnlockedAreaName;
}
