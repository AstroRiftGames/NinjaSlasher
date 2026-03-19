public enum PurchaseResult { Success, InsufficientFunds, InvalidData }

public static class PowerUpPurchaseService
{
    public static PurchaseResult Purchase(PowerUpType type, int cost)
    {
        if (SaveManager.Instance == null) return PurchaseResult.InvalidData;
        if (!SaveManager.Instance.SpendCoins(cost)) return PurchaseResult.InsufficientFunds;
        SaveManager.Instance.AddPowerUpToInventory(type, 1);
        return PurchaseResult.Success;
    }

    public static bool CanAfford(int cost)
        => SaveManager.Instance != null && SaveManager.Instance.GetCoins() >= cost;
}
