using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using System.Threading.Tasks;

public class IAPManager : MonoBehaviourSingleton<IAPManager>, IDetailedStoreListener
{
    [Header("Configuration")]
    [SerializeField] private List<IAPProductData> availableProducts;

    /// <summary>
    /// Broadcast fired for purchases that have no registered handler.
    /// Kept for backward compatibility — prefer RegisterPurchaseHandler for new code.
    /// </summary>
    public event Action<string> OnPurchaseCompleted;
    public event Action<string, string> OnPurchaseFailedEvent;
    public event Action OnIAPInitialized;
    public event Action<string> OnIAPInitializationFailed;

    private IStoreController _storeController;
    private IExtensionProvider _extensionProvider;
    private bool _isInitialized = false;

    // Per-product handlers. When a productId is registered here, ProcessPurchase
    // routes directly to its handler instead of broadcasting OnPurchaseCompleted.
    private readonly Dictionary<string, Action<PurchaseEventArgs>> _purchaseHandlers = new();

    public bool IsInitialized => _isInitialized;

    private async void Start()
    {
        await InitializeUnityServices();
        InitializeIAP();
    }

    private async Task InitializeUnityServices()
    {
        try
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[IAPManager] UnityServices state BEFORE: {UnityServices.State}");
#endif
            await UnityServicesInitializer.EnsureInitializedAsync();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[IAPManager] UnityServices state AFTER: {UnityServices.State}");
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[IAPManager] Error initializing Unity Services: {e}");
            OnIAPInitializationFailed?.Invoke(e.Message);
        }
    }

    private void InitializeIAP()
    {
        try
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            foreach (var product in availableProducts)
                builder.AddProduct(product.ProductId, product.ProductType);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[IAPManager] Starting IAP initialization...");
#endif
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

        Debug.Log("[IAPManager] IAP initialized");
        OnIAPInitialized?.Invoke();
    }

    public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        var msg = $"{error}" + (string.IsNullOrEmpty(message) ? "" : $": {message}");
        Debug.LogError($"[IAPManager] Initialization failed: {msg}");
        OnIAPInitializationFailed?.Invoke(msg);
    }

    // -------------------------------------------------------------------------
    // Purchase routing
    // -------------------------------------------------------------------------

    /// <summary>
    /// Registers a handler called when <paramref name="productId"/> is successfully purchased.
    /// The handler receives the full PurchaseEventArgs (useful for receipt access).
    /// One handler per productId; calling again replaces the previous entry.
    /// </summary>
    public void RegisterPurchaseHandler(string productId, Action<PurchaseEventArgs> handler)
    {
        _purchaseHandlers[productId] = handler;
    }

    /// <summary>Removes the handler for <paramref name="productId"/> if one is registered.</summary>
    public void UnregisterPurchaseHandler(string productId)
    {
        _purchaseHandlers.Remove(productId);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
    {
        var product   = purchaseEvent.purchasedProduct;
        var productId = product.definition.id;

        // Receipt validation — Android device only (editor uses fake receipts).
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!ValidateReceipt(purchaseEvent))
        {
            Debug.LogError($"[IAPManager] Receipt validation failed for '{productId}'. Rewards not granted.");
            return PurchaseProcessingResult.Complete;
        }
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[IAPManager] Purchase completed: {productId}");
#endif

        // Persist before dispatching so rewards survive a crash mid-delivery.
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.GetGameData().pendingPurchaseProductId = productId;
            SaveManager.Instance.SaveData();
        }

        // Route to a registered handler if available; otherwise broadcast for backward compat.
        if (_purchaseHandlers.TryGetValue(productId, out var handler))
            handler.Invoke(purchaseEvent);
        else
            OnPurchaseCompleted?.Invoke(productId);

        return PurchaseProcessingResult.Complete;
    }

    // -------------------------------------------------------------------------
    // Receipt validation (Android device builds only)
    // IMPORTANT: GooglePlayTangle and AppleTangle must be generated first via
    //            Window → Unity IAP → Receipt Validation Obfuscator.
    // -------------------------------------------------------------------------
#if UNITY_ANDROID && !UNITY_EDITOR
    private bool ValidateReceipt(PurchaseEventArgs purchaseEvent)
    {
        try
        {
            var validator = new CrossPlatformValidator(
                GooglePlayTangle.Data(),
                AppleTangle.Data(),
                Application.identifier);

            validator.Validate(purchaseEvent.purchasedProduct.receipt);
            return true;
        }
        catch (IAPSecurityException ex)
        {
            Debug.LogError($"[IAPManager] Receipt validation failed: {ex.Message}");
            return false;
        }
    }
#endif

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogError($"[IAPManager] Purchase failed: {product.definition.id}, reason: {failureReason}");
        OnPurchaseFailedEvent?.Invoke(product.definition.id, failureReason.ToString());
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[IAPManager] Starting purchase for: {productId}");
#endif
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
            Debug.LogError("[IAPManager] Cannot restore. IAP is not initialized.");
            callback?.Invoke(false, "IAP not initialized");
            return;
        }

        try
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[IAPManager] Restoring purchases");
#endif
#if UNITY_IOS
            var appleExtensions = _extensionProvider.GetExtension<IAppleExtensions>();
            appleExtensions.RestoreTransactions(success =>
            {
                if (success)
                {
                    Debug.Log("[IAPManager] Purchases restored successfully.");
                    callback?.Invoke(true, null);
                }
                else
                {
                    Debug.LogError("[IAPManager] Error restoring purchases.");
                    callback?.Invoke(false, "Restore failed");
                }
            });
#else
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
