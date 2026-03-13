using System;
using UnityEngine;
using UnityEngine.Purchasing;

[CreateAssetMenu(fileName = "NewProduct", menuName = "Store/Product Definition")]
public class StoreProductDefinition : ScriptableObject
{
    [Tooltip("Todos los IAP product IDs que mapean a este producto. Índice 0 = SKU primario.")]
    public string[] productIds = new string[0];

    [Tooltip("Tipo de producto IAP registrado en Google Play / App Store.")]
    public ProductType productType = ProductType.Consumable;

    public ProductCategory category;
    public RewardType      rewardType;

    [Header("Display (para overlays de emergencia)")]
    public BundleDisplayData display;

    [Header("Bundle Reward — rewardType = Bundle")]
    public BundleRewardData bundleReward;

    [Header("Coin Reward — rewardType = Coins")]
    [Min(0)] public int coinAmount;

    public string PrimaryProductId
        => productIds != null && productIds.Length > 0 ? productIds[0] : string.Empty;

    public bool MatchesProductId(string id)
        => productIds != null && Array.Exists(productIds, pid => pid == id);
}
