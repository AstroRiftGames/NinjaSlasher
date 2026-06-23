using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmergencyBundleModal : UIModalBase
{
    [Header("Bundle Info")]
    [SerializeField] private Image _bundleIcon;
    [SerializeField] private TMP_Text _bundleNameText;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private TMP_Text _rewardText;

    [Header("Countdown")]
    [SerializeField] private TMP_Text _countdownText;

    [Header("Buttons")]
    [SerializeField] private Button _buyBtn;
    [SerializeField] private Button _dismissBtn;

    private EmergencyBundleOffer _currentOffer;
    private Coroutine _countdownCoroutine;
    private bool _suppressDismissOnHide;
    private bool _isPurchasePending;

    public bool IsPurchasePending => _isPurchasePending;

    #region ENABLE / DISABLE

    protected override void OnEnable()
    {
        base.OnEnable();
        _buyBtn?.onClick.AddListener(OnBuyClicked);
        _dismissBtn?.onClick.AddListener(OnDismissClicked);
    }

    protected override void OnDisable()
    {
        ResetRuntimeStateForReuse();
        base.OnDisable();
        _buyBtn?.onClick.RemoveListener(OnBuyClicked);
        _dismissBtn?.onClick.RemoveListener(OnDismissClicked);
    }

    #endregion

    #region SHOW / HIDE

    public void ShowWithOffer(EmergencyBundleOffer offer)
    {
        ResetRuntimeStateForReuse();
        _currentOffer = offer;
        PopulateUI(offer);
        UpdateButtonStates();
    }

    protected override void OnShown()
    {
        base.OnShown();
        if (_currentOffer != null)
            _countdownCoroutine = StartCoroutine(CountdownRoutine(_currentOffer.OfferDurationSeconds));
    }

    protected override void OnHidden()
    {
        bool wasSuppressingDismissOnHide = _suppressDismissOnHide;
        ResetRuntimeStateForReuse();
        base.OnHidden();
        _currentOffer = null;

        if (wasSuppressingDismissOnHide)
        {
            EmergencyBundleService.Instance?.OnOfferClosedForTransition();
            return;
        }

        EmergencyBundleService.Instance?.OnOfferDismissed();
    }

    public void PrepareForFlowTransitionClose()
    {
        _suppressDismissOnHide = true;
    }

    public void PrepareForPurchaseResultClose()
    {
        _suppressDismissOnHide = true;
        _isPurchasePending = false;
        UpdateButtonStates();
    }

    public bool TryDismiss()
    {
        if (_isPurchasePending)
            return false;

        DismissOffer();
        return true;
    }

    public bool TryHandleBack()
    {
        return TryDismiss();
    }

    public bool IsShowingProduct(string productId)
    {
        return _currentOffer?.Product != null &&
               !string.IsNullOrEmpty(productId) &&
               string.Equals(_currentOffer.Product.PrimaryProductId, productId, System.StringComparison.Ordinal);
    }

    #endregion

    #region UI POPULATION

    private void PopulateUI(EmergencyBundleOffer offer)
    {
        var display = offer.Product?.display;
        var reward  = offer.Product?.bundleReward;

        if (_bundleIcon != null && display?.icon != null)
            _bundleIcon.sprite = display.icon;

        if (_bundleNameText != null)
            _bundleNameText.text = display?.bundleName ?? string.Empty;

        if (_priceText != null)
            _priceText.text = offer.LocalizedPrice;

        if (_rewardText != null)
            _rewardText.text = BuildRewardDescription(reward);
    }

    private string BuildRewardDescription(BundleRewardData reward)
    {
        if (reward == null) return string.Empty;

        var sb = new System.Text.StringBuilder();

        // Lives
        if (reward.unlimitedLives && reward.unlimitedLivesDurationMinutes > 0f)
            sb.Append($"Vidas ilimitadas {reward.unlimitedLivesDurationMinutes:0} min");
        else if (reward.regularLivesCount > 0)
            sb.Append($"+{reward.regularLivesCount} {(reward.regularLivesCount == 1 ? "vida" : "vidas")}");

        // Power-ups
        if (reward.powerUps != null)
        {
            foreach (var entry in reward.powerUps)
            {
                if (entry.quantity <= 0) continue;
                if (sb.Length > 0) sb.Append(" - ");
                sb.Append($"{GetPowerUpDisplayName(entry.type)} x{entry.quantity}");
            }
        }

        // Coins
        if (reward.coins > 0)
        {
            if (sb.Length > 0) sb.Append(" - ");
            sb.Append($"{reward.coins} monedas");
        }

        return sb.ToString();
    }

    private static string GetPowerUpDisplayName(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.ExtraTime:
                return "Tiempo Extra";
            case PowerUpType.DashTurbo:
                return "Turbo de Dash";
            case PowerUpType.ParryPerfect:
                return "Parry Perfecto";
            case PowerUpType.ComboMaster:
                return "Maestro del Combo";
            case PowerUpType.SecondChance:
                return "Segunda Oportunidad";
            case PowerUpType.HawkVision:
                return "Ojo de Halcón";
            case PowerUpType.EnhancedParry:
                return "Parry Potenciado";
            default:
                return type.ToString();
        }
    }

    #endregion

    #region COUNTDOWN

    private IEnumerator CountdownRoutine(float durationSeconds)
    {
        float remaining = durationSeconds;

        while (remaining > 0f)
        {
            if (_countdownText != null)
            {
                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = Mathf.FloorToInt(remaining % 60f);
                _countdownText.text = $"{minutes:D2}:{seconds:D2}";
            }

            yield return null;
            remaining -= Time.unscaledDeltaTime;
        }

        if (_countdownText != null) _countdownText.text = "00:00";
        OnCountdownExpired();
    }

    private void OnCountdownExpired()
    {
        if (_isPurchasePending)
            return;

        if (EmergencyBundleService.Instance != null)
            EmergencyBundleService.Instance.OnOfferExpired();
        else
            UIEvents.RequestHideEmergencyBundleModal();
    }

    private void StopCountdown()
    {
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }
    }

    #endregion

    #region BUTTON HANDLERS

    private void OnBuyClicked()
    {
        if (_currentOffer?.Product == null) return;

        string productId = _currentOffer.Product.PrimaryProductId;
        if (string.IsNullOrEmpty(productId)) return;

        IAPManager iapManager = IAPManager.Instance;
        if (iapManager == null)
        {
            Debug.LogWarning("[EmergencyBundleModal] IAPManager no disponible.");
            return;
        }

        if (!iapManager.IsInitialized)
        {
            ShowPurchaseUnavailableResult(productId);
            return;
        }

        var storeProduct = iapManager.GetProduct(productId);
        if (storeProduct == null || !storeProduct.availableToPurchase)
        {
            ShowPurchaseUnavailableResult(productId);
            return;
        }

        if (EmergencyBundleService.Instance != null && !EmergencyBundleService.Instance.TryBeginPurchase(productId))
            return;

        AnalyticsManager.Instance?.RecordPurchaseStarted(
            productId,
            AnalyticsManager.ProductCategoryStr(_currentOffer.Product.category),
            "paywall"
        );

        _isPurchasePending = true;
        UpdateButtonStates();

        PurchaseStartResult startResult = iapManager.PurchaseProduct(productId);
        if (startResult == PurchaseStartResult.Started)
            return;

        _isPurchasePending = false;
        UpdateButtonStates();
        ShowPurchaseStartRejectedResult(productId, startResult);
    }

    private void OnDismissClicked()
    {
        TryDismiss();
    }

    private void DismissOffer()
    {
        if (EmergencyBundleService.Instance != null)
            EmergencyBundleService.Instance.OnOfferDismissed();
        else
            UIEvents.RequestHideEmergencyBundleModal();
    }

    protected override void RequestCloseFromOutsideClick()
    {
        TryDismiss();
    }

    private void UpdateButtonStates()
    {
        if (_buyBtn != null)
            _buyBtn.interactable = !_isPurchasePending;

        if (_dismissBtn != null)
            _dismissBtn.interactable = !_isPurchasePending;
    }

    private void ResetRuntimeStateForReuse()
    {
        _isPurchasePending = false;
        _suppressDismissOnHide = false;
        StopCountdown();
        SetPanelInputEnabled(true);
        UpdateButtonStates();
    }

    private void ShowPurchaseUnavailableResult(string productId)
    {
        StoreProductDefinition product = StoreService.Instance?.GetProduct(productId);
        var request = new StorePurchaseResultRequest
        {
            ProductId = productId,
            ResultType = StorePurchaseResultType.Unavailable,
            Title = "Producto no disponible",
            Message = "Producto no disponible.",
            ConfirmButtonText = "Aceptar",
            Icon = product != null ? product.ConfirmationIcon : null
        };

        EmergencyBundleService.Instance?.OnStorePurchaseResultShown(request);
        UIEvents.RequestShowStorePurchaseResult(request);
    }

    private void ShowPurchaseStartRejectedResult(string productId, PurchaseStartResult startResult)
    {
        StoreProductDefinition product = StoreService.Instance?.GetProduct(productId);
        StorePurchaseResultRequest request = startResult switch
        {
            PurchaseStartResult.NotInitialized => BuildUnavailableRequest(productId, product),
            PurchaseStartResult.ProductUnavailable => BuildUnavailableRequest(productId, product),
            PurchaseStartResult.AlreadyProcessing => new StorePurchaseResultRequest
            {
                ProductId = productId,
                ResultType = StorePurchaseResultType.Failed,
                Title = "Compra en curso",
                Message = "Ya hay una compra en curso.",
                ConfirmButtonText = "Aceptar",
                Icon = product != null ? product.ConfirmationIcon : null
            },
            _ => new StorePurchaseResultRequest
            {
                ProductId = productId,
                ResultType = StorePurchaseResultType.Failed,
                Title = "Error de compra",
                Message = "No se pudo completar la compra.",
                ConfirmButtonText = "Aceptar",
                Icon = product != null ? product.ConfirmationIcon : null
            }
        };

        EmergencyBundleService.Instance?.OnStorePurchaseResultShown(request);
        UIEvents.RequestShowStorePurchaseResult(request);
    }

    private static StorePurchaseResultRequest BuildUnavailableRequest(string productId, StoreProductDefinition product)
    {
        return new StorePurchaseResultRequest
        {
            ProductId = productId,
            ResultType = StorePurchaseResultType.Unavailable,
            Title = "Producto no disponible",
            Message = "Producto no disponible.",
            ConfirmButtonText = "Aceptar",
            Icon = product != null ? product.ConfirmationIcon : null
        };
    }

    #endregion
}
