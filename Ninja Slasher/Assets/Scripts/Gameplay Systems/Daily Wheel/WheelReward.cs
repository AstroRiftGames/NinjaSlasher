using System;
using UnityEngine;

[Serializable]
public enum WheelRewardType
{
    PowerUp,
    FreeSpin
}

[Serializable]
public class WheelReward
{
    public WheelRewardType rewardType;
    public PowerUpType powerUpType;
    public int quantity;
    public Sprite icon;
    public string displayName;
    public Color backgroundColor = Color.white;

    [Range(0f, 1f)]
    public float weight = 1f;
}
