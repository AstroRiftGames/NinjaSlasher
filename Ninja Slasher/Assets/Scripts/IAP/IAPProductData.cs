using UnityEngine;
using UnityEngine.Purchasing;

[CreateAssetMenu(fileName = "NewIAPProduct", menuName = "IAP Product Data")]
public class IAPProductData : ScriptableObject
{
    [Tooltip("ID Google Play Console")]
    [SerializeField] private string productId;
    [SerializeField] private ProductType productType;

    public string ProductId => productId;
    public ProductType ProductType => productType;
}