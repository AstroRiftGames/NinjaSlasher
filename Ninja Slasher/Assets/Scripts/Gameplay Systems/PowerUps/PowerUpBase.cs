using UnityEngine;

public abstract class PowerUpBase : ScriptableObject
{
    public PowerUpType powerUpType;

    [Header("CONFIGURATION")]
    [Tooltip("Referencia al GameConfig para obtener balance centralizado")]
    public GameConfig gameConfig;

    [Header("VISUAL")]
    public string displayName;
    public string description;
    public Sprite icon;

    public int maxUses
    {
        get
        {
            if (gameConfig == null)
            {
                return 10;
            }
            return gameConfig.GetPowerUpUses(powerUpType);
        }
    }

    public int cost => gameConfig != null ? gameConfig.GetPowerUpCost(powerUpType) : 0;

    public abstract void Activate(PowerUpContext context);
    public abstract void Deactivate(PowerUpContext context);
    public abstract void OnUseConsumed(PowerUpContext context);
}