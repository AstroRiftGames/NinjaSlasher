using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using UnityEngine.Purchasing;

public class IAPManager : MonoBehaviourSingleton<IAPManager>, IDetailedStoreListener
{
    [Header("Configuration")]
    [SerializeField] private List<IAPProductData> availableProducts;

    public event Action<string> OnPurchaseCompleted;
    public event Action<string, string> OnPurchaseFailedEvent;
    public event Action OnIAPInitialized;
    public event Action<string> OnIAPInitializationFailed;

    private IStoreController _storeController;
    private IExtensionProvider _extensionProvider;
    private bool _isInitialized = false;

    public bool IsInitialized => _isInitialized;

    private async void Start()
    {
        await InitializeUnityServices();
        InitializeIAP();
    }

    private async System.Threading.Tasks.Task InitializeUnityServices()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
                Debug.Log("[IAPManager] Unity Services inicialized.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[IAPManager] Error initializing Unity Services: {e.Message}");
            OnIAPInitializationFailed?.Invoke(e.Message);
        }
    }

    private void InitializeIAP()
    {
        try
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            foreach (var product in availableProducts)
            {
                builder.AddProduct(product.ProductId, product.ProductType);
            }

            Debug.Log("[IAPManager] Starting IAP initialization...");
            UnityPurchasing.Initialize(this, builder);
        }
        catch (Exception e)
        {
            Debug.LogError($"[IAPManager] Error during IAP initialization: {e.Message}");
            OnIAPInitializationFailed?.Invoke(e.Message);
        }
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        _storeController = controller;
        _extensionProvider = extensions;
        _isInitialized = true;

        Debug.Log("[IAPManager] IAP inicialized");
        OnIAPInitialized?.Invoke();
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        OnInitializeFailed(error, null);
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        var errorMessage = $"{error}" + (string.IsNullOrEmpty(message) ? "" : $": {message}");
        Debug.LogError($"[IAPManager] Inicialization failed: {errorMessage}");
        OnIAPInitializationFailed?.Invoke(errorMessage);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
    {
        var product = purchaseEvent.purchasedProduct;
        var productId = product.definition.id;

        Debug.Log($"[IAPManager] Purchase completed: {productId}");
        OnPurchaseCompleted?.Invoke(productId);

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        var productId = product.definition.id;
        Debug.LogError($"[IAPManager] Purchase failed: {productId}, reason: {failureReason}");
        OnPurchaseFailedEvent?.Invoke(productId, failureReason.ToString());
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        OnPurchaseFailed(product, failureDescription.reason);
    }

    public void PurchaseProduct(string productId)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[IAPManager] The purchase cannot be completed. IAP is not initialized");
            OnPurchaseFailedEvent?.Invoke(productId, "IAP not initialized");
            return;
        }

        try
        {
            var product = _storeController.products.WithID(productId);

            if (product != null && product.availableToPurchase)
            {
                Debug.Log($"[IAPManager] Starting purchase for: {productId}");
                _storeController.InitiatePurchase(product);
            }
            else
            {
                Debug.LogError($"[IAPManager] Product not found or unavailable: {productId}");
                OnPurchaseFailedEvent?.Invoke(productId, "Product not available");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[IAPManager] Error when starting purchase: {e.Message}");
            OnPurchaseFailedEvent?.Invoke(productId, e.Message);
        }
    }

    public Product GetProduct(string productId)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[IAPManager] IAP is not initialized");
            return null;
        }

        return _storeController.products.WithID(productId);
    }

    public string GetProductPrice(string productId)
    {
        var product = GetProduct(productId);
        return product?.metadata.localizedPriceString ?? "N/A";
    }

    public void RestorePurchases(Action<bool, string> callback = null)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[IAPManager] Cannot be restored. IAP is not initialized.");
            callback?.Invoke(false, "IAP not initialized");
            return;
        }

        try
        {
            Debug.Log("[IAPManager] Restoring purchases");

            // Para iOS
#if UNITY_IOS
            var appleExtensions = _extensionProvider.GetExtension<IAppleExtensions>();
            appleExtensions.RestoreTransactions((success) =>
            {
                if (success)
                {
                    Debug.Log("[IAPManager] Compras restauradas exitosamente.");
                    callback?.Invoke(true, null);
                }
                else
                {
                    Debug.LogError("[IAPManager] Error al restaurar compras.");
                    callback?.Invoke(false, "Restore failed");
                }
            });
#else
            Debug.Log("[IAPManager] Restore is not necessary on this platform.");
            callback?.Invoke(true, null);
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[IAPManager] Error restoring purchases: {e.Message}");
            callback?.Invoke(false, e.Message);
        }
    }
}