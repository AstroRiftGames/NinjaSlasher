using UnityEngine;

[CreateAssetMenu(fileName = "NewWheelRewardSet", menuName = "Daily Wheel/Reward Set")]
public class WheelRewardSet : ScriptableObject
{
    public WheelReward[] rewards;
}
