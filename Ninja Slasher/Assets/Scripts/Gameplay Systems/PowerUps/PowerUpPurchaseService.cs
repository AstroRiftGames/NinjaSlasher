public enum PurchaseResult { Success, InsufficientFunds, InvalidData }

public static class PowerUpPurchaseService
{
    public static PurchaseResult Purchase(PowerUpType type, int cost)
    {
        if (SaveManager.Instance == null) return PurchaseResult.InvalidData;
        if (!SaveManager.Instance.SpendCoins(cost)) return PurchaseResult.InsufficientFunds;

        GameConfig config = GameConfigManager.GetConfig();
        int quantity = config != null ? config.GetPowerUpGrantQuantity(type) : 1;
        SaveManager.Instance.AddPowerUpToInventory(type, quantity);
        return PurchaseResult.Success;
    }

    public static bool CanAfford(int cost)
        => SaveManager.Instance != null && SaveManager.Instance.GetCoins() >= cost;
}
