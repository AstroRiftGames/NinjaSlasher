using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public enum ProductCategory
{
    Bundle,
    CoinPack
}

[Serializable]
public class StoreProductDefinition
{
    [Tooltip("Todos los IAP product IDs que mapean a este producto")]
    public string[] productIds = new string[0];

    [Tooltip("Tipo de producto IAP registrado")]
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

[CreateAssetMenu(fileName = "StoreCatalog", menuName = "Store/Store Catalog")]
public class StoreCatalog : ScriptableObject
{
    public List<StoreProductDefinition> products = new();

    public StoreProductDefinition GetByProductId(string productId)
    {
        foreach (var p in products)
            if (p.MatchesProductId(productId)) return p;
        return null;
    }

    public StoreProductDefinition GetByCategory(ProductCategory category)
        => products.Find(p => p.category == category);

    public List<StoreProductDefinition> GetAllByCategory(ProductCategory category)
        => products.FindAll(p => p.category == category);

    public IEnumerable<string> GetAllProductIds()
    {
        foreach (var p in products)
            if (p.productIds != null)
                foreach (var id in p.productIds)
                    if (!string.IsNullOrEmpty(id)) yield return id;
    }

    private void OnValidate()
    {
        var ids = new HashSet<string>();

        foreach (var p in products)
        {
            if (p.productIds == null) continue;

            foreach (var id in p.productIds)
            {
                if (string.IsNullOrEmpty(id)) continue;

                if (!ids.Add(id))
                    Debug.LogError($"Duplicate productId in StoreCatalog: {id}", this);
            }
        }
    }
}
