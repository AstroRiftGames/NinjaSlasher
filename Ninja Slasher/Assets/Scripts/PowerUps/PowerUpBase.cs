using UnityEngine;

public abstract class PowerUpBase : ScriptableObject
{
    public PowerUpType powerUpType;
    public float duration = 3600f;

    public string displayName;
    public string description;
    public Sprite icon;

    public abstract void Activate(PowerUpContext context);
    public abstract void Deactivate(PowerUpContext context);
}
