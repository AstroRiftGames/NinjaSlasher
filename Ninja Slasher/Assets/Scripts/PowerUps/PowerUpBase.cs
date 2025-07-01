using UnityEngine;

public abstract class PowerUpBase : ScriptableObject
{
    public PowerUpType powerUpType;
    public float duration = 3600f;

    public abstract void Activate(PowerUpContext context);
    public abstract void Deactivate(PowerUpContext context);
}
