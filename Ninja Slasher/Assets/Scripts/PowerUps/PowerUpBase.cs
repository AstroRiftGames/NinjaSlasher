using UnityEngine;

public abstract class PowerUpBase : ScriptableObject
{
    public PowerUpType powerUpType;

    [Header("USES")]
    [Tooltip("Cantidad de usos que tiene este power-up cuando se activa")]
    public int maxUses = 3;

    [Header("VISUAL")]
    public string displayName;
    public string description;
    public Sprite icon;

    public abstract void Activate(PowerUpContext context);
    public abstract void Deactivate(PowerUpContext context);
    public abstract void OnUseConsumed(PowerUpContext context);
}