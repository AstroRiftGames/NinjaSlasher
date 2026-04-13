using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using System.Threading.Tasks;

public class IAPManager : MonoBehaviourSingleton<IAPManager>, IDetailedStoreListener
{
    public event Action<string> OnPurchaseCompleted;
    public event Action<string, string> OnPurchaseFailedEvent;
    public event Action OnIAPInitialized;
    public event Action<string> OnIAPInitializationFailed;

    private IStoreController _storeController;
    private IExtensionProvider _extensionProvider;
    private bool _isInitialized = false;
    private bool _isInitializing = false;
    private PurchaseState _purchaseState = PurchaseState.Idle;

    private readonly Dictionary<string, Action<PurchaseEventArgs>> _purchaseHandlers = new();
    private HashSet<string> _processedTransactions = new();
    public bool IsInitialized => _isInitialized;
    public PurchaseState PurchaseState => _purchaseState;

    public async void Initialize(StoreCatalog catalog)
    {
        if (_isInitialized || _isInitializing) return;

        if (catalog == null)
        {
            Debug.LogError("[IAPManager] Initialize: catalog is null.");
            return;
        }

        _isInitializing = true;

        await InitializeUnityServices();
        InitializeIAP(catalog);
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

    private void InitializeIAP(StoreCatalog catalog)
    {
        try
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            foreach (var product in catalog.products)
            {
                if (product.productIds == null) continue;
                foreach (var id in product.productIds)
                    if (!string.IsNullOrEmpty(id))
                        builder.AddProduct(id, product.productType);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[IAPManager] Starting IAP initialization...");
#endif
            UnityPurchasing.Initialize(this, builder);
        }
        catch (Exception e)
        {
            Debug.LogError($"[IAPManager] Error during IAP initialization: {e.Message}");
            _isInitializing = false;
            OnIAPInitializationFailed?.Invoke(e.Message);
        }
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        _storeController = controller;
        _extensionProvider = extensions;
        _isInitialized = true;
        _isInitializing = false;

        Debug.Log($"[IAPManager] IAP initialized with {controller.products.all.Length} store products.");
        OnIAPInitialized?.Invoke();
    }

    public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        _isInitializing = false;
        var msg = $"{error}" + (string.IsNullOrEmpty(message) ? "" : $": {message}");
        Debug.LogError($"[IAPManager] Initialization failed: {msg}");
        OnIAPInitializationFailed?.Invoke(msg);
    }

    public void RegisterPurchaseHandler(string productId, Action<PurchaseEventArgs> handler)
    {
        _purchaseHandlers[productId] = handler;
    }

    public void UnregisterPurchaseHandler(string productId)
    {
        _purchaseHandlers.Remove(productId);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
    {
        var product = purchaseEvent.purchasedProduct;
        var productId = product.definition.id;
        var transactionId = product.transactionID;

        Debug.Log($"[IAPManager] ProcessPurchase: '{productId}' | tx={transactionId}");

        if (_processedTransactions.Contains(transactionId))
        {
            Debug.LogWarning($"[IAPManager] Duplicate transaction ignored: {transactionId}");
            return PurchaseProcessingResult.Complete;
        }

        _processedTransactions.Add(transactionId);

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!ValidateReceipt(purchaseEvent))
        {
            Debug.LogWarning($"[IAPManager] Receipt validation failed. Purchase blocked: '{productId}'");
            _purchaseState = PurchaseState.Idle;
            SaveManager.Instance?.Modify(d => d.pendingPurchaseProductId = "");
            return PurchaseProcessingResult.Complete;
        }
#endif

        if (_purchaseHandlers.TryGetValue(productId, out var handler))
        {
            handler.Invoke(purchaseEvent);
        }
        else
        {
            OnPurchaseCompleted?.Invoke(productId);
        }

        _purchaseState = PurchaseState.Idle;

        return PurchaseProcessingResult.Complete;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private bool ValidateReceipt(PurchaseEventArgs purchaseEvent)
    {
        try
        {
            var validator = new CrossPlatformValidator(
                GooglePlayTangle.Data(),
                null, // AppleTangle no generado: irrelevante en Android
                Application.identifier);

            validator.Validate(purchaseEvent.purchasedProduct.receipt);
            return true;
        }
        catch (IAPSecurityException ex)
        {
            // Legitimate fraud / tampered receipt — block the reward.
            Debug.LogError($"[IAPManager] Receipt validation failed (security): {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            // Validation infrastructure failure (missing tangle files, wrong key, etc.)
            // Grant the purchase so a paying user is never penalized for a config error.
            Debug.LogError($"[IAPManager] Receipt validation error ({ex.GetType().Name}): {ex.Message}. Granting purchase.");
            return true;
        }
    }
#endif

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        _purchaseState = PurchaseState.Idle;
        SaveManager.Instance?.Modify(d => d.pendingPurchaseProductId = "");
        Debug.LogError($"[IAPManager] Purchase failed: {product.definition.id}, reason: {failureReason}");
        OnPurchaseFailedEvent?.Invoke(product.definition.id, failureReason.ToString());
        AnalyticsManager.Instance?.RecordPurchaseFailed(product.definition.id, failureReason.ToString());
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        OnPurchaseFailed(product, failureDescription.reason);
    }

    public void PurchaseProduct(string productId)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning($"[IAPManager] Purchase requested before initialization for '{productId}'.");
            return;
        }

        var product = _storeController.products.WithID(productId);

        if (product != null && product.availableToPurchase)
        {
            var data = SaveManager.Instance.GetGameData();
            data.pendingPurchaseProductId = productId;
            SaveManager.Instance.SaveData();

            _purchaseState = PurchaseState.Processing;
            _storeController.InitiatePurchase(product);
        }
        else
        {
            Debug.LogWarning($"[IAPManager] Product '{productId}' is missing from the store response or not available to purchase.");
        }
    }

    public Product GetProduct(string productId)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[IAPManager] IAP is not initialized");
            return null;
        }

        Product product = _storeController.products.WithID(productId);

        if (product == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[IAPManager] Store product not found for id '{productId}'.");
#endif
            return null;
        }

        return product;
    }

    public string GetProductPrice(string productId)
    {
        var product = GetProduct(productId);

        if (product?.metadata == null || string.IsNullOrEmpty(product.metadata.localizedPriceString))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[IAPManager] Localized price unavailable for '{productId}'.");
#endif
            return "N/A";
        }

        return product.metadata.localizedPriceString;
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
                    AnalyticsManager.Instance?.RecordPurchaseRestored();
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
