using System.Collections.Generic;
using UnityEngine;

public enum ProductCategory
{
    Bundle,
    CoinPack
}

[CreateAssetMenu(fileName = "StoreCatalog", menuName = "Store/Store Catalog")]
public class StoreCatalog : ScriptableObject
{
    private static readonly Dictionary<string, string> LegacyProductIdAliases = new()
    {
        { "bento_medium", "medium_bento" },
        { "ninja_coins_bronze", "coins_small" },
        { "ninja_coins_silver", "coins_medium" },
        { "ninja_coins_gold", "coins_large" },
    };

    public List<StoreProductDefinition> products = new();

    public StoreProductDefinition GetByProductId(string productId)
    {
        string normalizedProductId = NormalizeProductId(productId);

        foreach (var p in products)
            if (p != null && p.MatchesProductId(normalizedProductId)) return p;
        return null;
    }

    public StoreProductDefinition GetByCategory(ProductCategory category)
        => products.Find(p => p != null && p.category == category);

    public List<StoreProductDefinition> GetAllByCategory(ProductCategory category)
        => products.FindAll(p => p != null && p.category == category);

    public IEnumerable<string> GetAllProductIds()
    {
        foreach (var p in products)
        {
            if (p == null || p.productIds == null) continue;
            foreach (var id in p.productIds)
                if (!string.IsNullOrEmpty(id)) yield return id;
        }
    }

    public string NormalizeProductId(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            return string.Empty;

        return LegacyProductIdAliases.TryGetValue(productId, out string normalizedProductId)
            ? normalizedProductId
            : productId;
    }

    private void OnValidate()
    {
        var ids = new HashSet<string>();

        foreach (var p in products)
        {
            if (p == null || p.productIds == null) continue;

            foreach (var id in p.productIds)
            {
                if (string.IsNullOrEmpty(id)) continue;

                if (!ids.Add(id))
                    Debug.LogError($"Duplicate productId in StoreCatalog: {id}", this);
            }
        }
    }
}
