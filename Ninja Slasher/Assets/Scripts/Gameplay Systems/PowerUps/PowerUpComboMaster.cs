using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/ComboMaster")]
public class PowerUpComboMaster : PowerUpBase
{
    public override void Activate(PowerUpContext context)
    {
        context.ComboMasterActive = true;
        context.ComboBonusPercent = gameConfig != null ? gameConfig.GetComboMasterBonusPercent() : 0.5f;
    }

    public override void Deactivate(PowerUpContext context)
    {
        context.ComboMasterActive = false;
        context.ComboBonusPercent = 0f;
    }

}
