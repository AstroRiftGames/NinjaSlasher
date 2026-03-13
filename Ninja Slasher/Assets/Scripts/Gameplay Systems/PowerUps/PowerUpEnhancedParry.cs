using UnityEngine;

[CreateAssetMenu(fileName = "PowerUpEnhancedParry", menuName = "PowerUps/EnhancedParry")]
public class PowerUpEnhancedParry : PowerUpBase
{
    [SerializeField] private int bouncesAmount = 3;
    [SerializeField][Range(0.5f, 1f)] private float velocityRetentionPerBounce = 0.9f;

    public override void Activate(PowerUpContext context)
    {
        context.EnhancedParryActive = true;
        context.EnhancedParryBounces = bouncesAmount;
        context.EnhancedParryVelocityRetention = velocityRetentionPerBounce;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.EnhancedParryActive = false;
        context.EnhancedParryBounces = bouncesAmount;
        context.EnhancedParryVelocityRetention = velocityRetentionPerBounce;
        context.EnhancedParryUsesRemaining = 0;
    }

    public override void OnUseConsumed(PowerUpContext context)
    {
        Debug.Log($"[EnhancedParry] Uso consumido. Restantes: {context.EnhancedParryUsesRemaining}");

        if (context.EnhancedParryUsesRemaining == 1)
        {
            Debug.LogWarning("[EnhancedParry] Ultimo uso disponible");
        }
    }
}