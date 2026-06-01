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

    public int cost => gameConfig != null ? gameConfig.GetPowerUpCost(powerUpType) : 0;

    public abstract void Activate(PowerUpContext context);
    public abstract void Deactivate(PowerUpContext context);
}