using System;

[Serializable]
public class LevelStats
{
    public float timeTaken;
    public int enemiesDefeated;
    public int totalEnemies;
    public int movesUsed;
    public bool parryKillDone;
    public int starsEarned;

    // Nuevos objetivos
    public float maxComboActiveDuration;
    public int reflectedProjectileKills;
    public int maxComboLevelReached;
}