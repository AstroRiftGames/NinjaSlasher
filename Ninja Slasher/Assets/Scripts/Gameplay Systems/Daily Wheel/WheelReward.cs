using System;
using UnityEngine;

[Serializable]
public class WheelReward
{
    public PowerUpType powerUpType;
    public int quantity;
    public Sprite icon;
    public string displayName;
    public Color backgroundColor = Color.white;

    [Range(0f, 1f)]
    public float weight = 1f;
}

[Serializable]
public class WheelData
{
    public string lastSpinDate;
    public int totalSpins;
    public int consecutiveSpins;

    public WheelData()
    {
        lastSpinDate = string.Empty;
        totalSpins = 0;
        consecutiveSpins = 0;
    }
}