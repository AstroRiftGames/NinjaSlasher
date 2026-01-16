using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/ComboMaster")]
public class PowerUpComboMaster : PowerUpBase
{
    [Range(0f, 2f)] public float comboBonusPercent;

    public override void Activate(PowerUpContext context)
    {
        context.ComboMasterActive = true;
        context.ComboBonusPercent = comboBonusPercent;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.ComboMasterActive = false;
        context.ComboBonusPercent = 0f;
    }

    public override void OnUseConsumed(PowerUpContext context)
    {
        Debug.Log($"[DashTurbo] Uso consumido. Restantes: {context.DashTurboUsesRemaining}");

        if (context.ComboMasterUsesRemaining == 1)
        {
            Debug.LogWarning("[DashTurbo] Ultimo uso disponible");
        }
    }
}
