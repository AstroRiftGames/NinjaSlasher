using UnityEngine;

[CreateAssetMenu(fileName = "BossPreGameData", menuName = "Game/UI/Boss PreGame Data")]
public class BossPreGameData : ScriptableObject
{
    [Header("BOSS INFO")]
    public string bossName;

    [TextArea(4, 8)]
    public string loreDescription;

    [Header("BOSS VICTORY")]
    [TextArea(2, 4)]
    public string victoryMessage;

    public string bossUnlockSubtitle;
    public string bossUnlockDetail;

    public Sprite bossPortrait;
}
