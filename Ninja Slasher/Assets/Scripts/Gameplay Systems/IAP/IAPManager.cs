using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Unity.Services.Core;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using System.Threading.Tasks;

public static class PurchaseDedupeKeyUtility
{
    public static bool TryBuild(string transactionId, string receipt, out string dedupeKey)
    {
        if (!string.IsNullOrWhiteSpace(transactionId))
        {
            dedupeKey = $"tx:{transactionId}";
            return true;
        }

        if (string.IsNullOrWhiteSpace(receipt))
        {
            dedupeKey = null;
            return false;
        }

        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(receipt));
        StringBuilder builder = new StringBuilder(hashBytes.Length * 2 + 8);
        builder.Append("rcpt:");
        for (int i = 0; i < hashBytes.Length; i++)
            builder.Append(hashBytes[i].ToString("x2"));

        dedupeKey = builder.ToString();
        return true;
    }
}

public class IAPManager : MonoBehaviourSingleton<IAPManager>, IDetailedStoreListener
{
    public event Action<string> OnPurchaseCompleted;
    public event Action<string, PurchaseFailureReason> OnPurchaseFailedEvent;
    public event Action OnIAPInitialized;
    public event Action<string> OnIAPInitializationFailed;

    private IStoreController _storeController;
    private IExtensionProvider _extensionProvider;
    private bool _isInitialized = false;
    private bool _isInitializing = false;
    private PurchaseState _purchaseState = PurchaseState.Idle;

    private readonly Dictionary<string, Func<PurchaseEventArgs, StorePurchaseProcessingResult>> _purchaseHandlers = new();
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[IAPManager] IAP initialized with {controller.products.all.Length} store products.");
#endif
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

    public void RegisterPurchaseHandler(string productId, Func<PurchaseEventArgs, StorePurchaseProcessingResult> handler)
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
        var receipt = product.receipt;
        PurchaseDedupeKeyUtility.TryBuild(transactionId, receipt, out string callbackDedupeKey);

        Debug.Log($"[IAPManager] ProcessPurchase received | productId='{productId}' | tx='{transactionId}' | hasReceipt={!string.IsNullOrEmpty(receipt)} | dedupeKey='{callbackDedupeKey}'");

        if (!string.IsNullOrWhiteSpace(callbackDedupeKey) && _processedTransactions.Contains(callbackDedupeKey))
        {
            Debug.LogWarning($"[IAPManager] Duplicate purchase ignored in-session: '{callbackDedupeKey}'");
            return PurchaseProcessingResult.Complete;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!ValidateReceipt(purchaseEvent))
        {
            Debug.LogWarning($"[IAPManager] Receipt validation failed. Purchase blocked: '{productId}'");
            _purchaseState = PurchaseState.Idle;
            SaveManager.Instance?.ClearPendingPurchaseProductId();
            return PurchaseProcessingResult.Complete;
        }
#endif

        StorePurchaseProcessingResult processingResult;

        if (_purchaseHandlers.TryGetValue(productId, out var handler))
        {
            processingResult = handler.Invoke(purchaseEvent);
        }
        else
        {
            OnPurchaseCompleted?.Invoke(productId);
            processingResult = StorePurchaseProcessingResult.RejectAndComplete($"No registered purchase handler for '{productId}'.");
        }

        switch (processingResult.Decision)
        {
            case StorePurchaseProcessingDecision.Complete:
                if (!string.IsNullOrWhiteSpace(processingResult.ProcessedPurchaseKey))
                {
                    _processedTransactions.Add(processingResult.ProcessedPurchaseKey);
                    Debug.Log($"[IAPManager] Purchase completed durably | key='{processingResult.ProcessedPurchaseKey}'");
                }
                _purchaseState = PurchaseState.Idle;
                return PurchaseProcessingResult.Complete;

            case StorePurchaseProcessingDecision.Pending:
                Debug.LogWarning($"[IAPManager] Purchase kept pending until durable persistence succeeds | productId='{productId}' | key='{processingResult.ProcessedPurchaseKey}'");
                return PurchaseProcessingResult.Pending;

            default:
                _purchaseState = PurchaseState.Idle;
                Debug.LogWarning($"[IAPManager] Purchase callback rejected and completed | productId='{productId}' | reason='{processingResult.Message}'");
                return PurchaseProcessingResult.Complete;
        }
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
        SaveManager.Instance?.ClearPendingPurchaseProductId();
        Debug.LogWarning($"[IAPManager] Purchase failed callback | productId='{product.definition.id}' | reason={failureReason}");
        OnPurchaseFailedEvent?.Invoke(product.definition.id, failureReason);
        AnalyticsManager.Instance?.RecordPurchaseFailed(product.definition.id, failureReason.ToString());
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        OnPurchaseFailed(product, failureDescription.reason);
    }

    public PurchaseStartResult PurchaseProduct(string productId)
    {
        if (_purchaseState == PurchaseState.Processing)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[IAPManager] Purchase already in progress. Ignoring request for '{productId}'.");
#endif
            return PurchaseStartResult.AlreadyProcessing;
        }

        if (!_isInitialized)
        {
            Debug.LogWarning($"[IAPManager] Purchase requested before initialization for '{productId}'.");
            return PurchaseStartResult.NotInitialized;
        }

        var product = _storeController.products.WithID(productId);

        if (product != null && product.availableToPurchase)
        {
            _purchaseState = PurchaseState.Processing;
            Debug.Log($"[IAPManager] Initiating purchase | productId='{productId}'");
            _storeController.InitiatePurchase(product);
            return PurchaseStartResult.Started;
        }

        Debug.LogWarning($"[IAPManager] Product '{productId}' is missing from the store response or not available to purchase.");
        return PurchaseStartResult.ProductUnavailable;
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
