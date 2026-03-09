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
    public List<StoreProductDefinition> products = new();

    public StoreProductDefinition GetByProductId(string productId)
    {
        foreach (var p in products)
            if (p != null && p.MatchesProductId(productId)) return p;
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
