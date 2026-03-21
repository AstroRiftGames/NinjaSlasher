using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StorePurchaseConfirmationPanel : UIPopupBase
{
    private const string PurchaseIconName = "PurchaseIcon";
    private const string PurchaseNameName = "PurchaseName";
    private const string DescriptionName = "Description";
    private const string BuyButtonName = "BuyButton";
    private const string CancelButtonName = "CancelButton";

    [Header("View References")]
    [SerializeField] private Image _purchaseIcon;
    [SerializeField] private TextMeshProUGUI _purchaseNameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Button _buyButton;
    [SerializeField] private Button _cancelButton;

    private string _pendingProductId;
    private RectTransform _pendingFeedbackOrigin;
    private bool _isInitialized;

    protected override void Awake()
    {
        base.Awake();
        InitializeIfNeeded();
    }

    public void ShowConfirmation(string productId, RectTransform feedbackOrigin)
    {
        InitializeIfNeeded();

        if (!HasRequiredBindings())
        {
            Debug.LogWarning("[StorePurchaseConfirmationPanel] Missing required references on StorePurchaseConfirmationPopUp.");
            return;
        }

        if (string.IsNullOrEmpty(productId))
        {
            Debug.LogWarning("[StorePurchaseConfirmationPanel] Cannot show confirmation for an empty product id.");
            return;
        }

        StoreProductDefinition product = StoreService.Instance?.GetProduct(productId);
        if (product == null)
        {
            Debug.LogWarning($"[StorePurchaseConfirmationPanel] Product not found for id '{productId}'.");
            return;
        }

        _pendingProductId = productId;
        _pendingFeedbackOrigin = feedbackOrigin;

        ApplyProduct(product);
        Show();
    }

    public void HideImmediate()
    {
        DOTween.Kill(_panelTransform);

        _isVisible = false;
        SetPanelInputEnabled(false);

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        if (_panelTransform != null)
            _panelTransform.localScale = Vector3.one;

        ResetViewState();
        gameObject.SetActive(false);
    }

    protected override void OnHidden()
    {
        ResetViewState();
        base.OnHidden();
    }

    private void SetupButtons()
    {
        if (_buyButton != null)
        {
            _buyButton.onClick.RemoveListener(OnBuyClicked);
            _buyButton.onClick.AddListener(OnBuyClicked);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.RemoveListener(OnCancelClicked);
            _cancelButton.onClick.AddListener(OnCancelClicked);
        }
    }

    private void InitializeIfNeeded()
    {
        if (_isInitialized)
            return;

        ResolveBindings();
        SetupButtons();
        ResetViewState();
        _isInitialized = true;
    }

    private bool HasRequiredBindings()
    {
        return _panelTransform != null &&
               _canvasGroup != null &&
               _purchaseIcon != null &&
               _purchaseNameText != null &&
               _descriptionText != null &&
               _buyButton != null &&
               _cancelButton != null;
    }

    private void ResolveBindings()
    {
        if (_panelTransform == null)
        {
            CanvasGroup panelCanvasGroup = GetComponentInChildren<CanvasGroup>(true);
            if (panelCanvasGroup != null)
                _panelTransform = panelCanvasGroup.transform as RectTransform;
        }

        if (_canvasGroup == null && _panelTransform != null)
            _canvasGroup = _panelTransform.GetComponent<CanvasGroup>();

        _purchaseIcon = ResolveComponent(_purchaseIcon, PurchaseIconName);
        _purchaseNameText = ResolveComponent(_purchaseNameText, PurchaseNameName);
        _descriptionText = ResolveComponent(_descriptionText, DescriptionName);
        _buyButton = ResolveComponent(_buyButton, BuyButtonName);
        _cancelButton = ResolveComponent(_cancelButton, CancelButtonName);
    }

    private T ResolveComponent<T>(T currentReference, string objectName) where T : Component
    {
        if (currentReference != null)
            return currentReference;

        Transform target = FindDeepChild(transform, objectName);
        if (target == null)
            return null;

        return target.GetComponent<T>();
    }

    private static Transform FindDeepChild(Transform parent, string objectName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == objectName)
                return child;

            Transform match = FindDeepChild(child, objectName);
            if (match != null)
                return match;
        }

        return null;
    }

    private void ApplyProduct(StoreProductDefinition product)
    {
        if (_purchaseNameText != null)
        {
            _purchaseNameText.gameObject.SetActive(true);
            _purchaseNameText.text = GetDisplayName(product);
        }

        if (_descriptionText != null)
        {
            _descriptionText.gameObject.SetActive(true);
            _descriptionText.text = BuildDescription(product);
        }

        if (_purchaseIcon != null)
        {
            Sprite icon = product.display != null ? product.display.icon : null;
            _purchaseIcon.gameObject.SetActive(true);
            _purchaseIcon.sprite = icon;
            _purchaseIcon.enabled = icon != null;
        }
    }

    private void OnBuyClicked()
    {
        string productId = _pendingProductId;
        RectTransform feedbackOrigin = _pendingFeedbackOrigin;

        Hide();

        if (string.IsNullOrEmpty(productId))
            return;

        StoreService.Instance?.Buy(productId, feedbackOrigin);
    }

    private void OnCancelClicked()
    {
        Hide();
    }

    private void ResetViewState()
    {
        _pendingProductId = null;
        _pendingFeedbackOrigin = null;

        if (_purchaseNameText != null)
            _purchaseNameText.text = string.Empty;

        if (_descriptionText != null)
            _descriptionText.text = string.Empty;

        if (_purchaseIcon != null)
        {
            _purchaseIcon.sprite = null;
            _purchaseIcon.enabled = false;
        }
    }

    private static string GetDisplayName(StoreProductDefinition product)
    {
        if (product == null)
            return string.Empty;

        if (product.display != null && !string.IsNullOrWhiteSpace(product.display.bundleName))
            return product.display.bundleName;

        return Humanize(product.PrimaryProductId);
    }

    private static string BuildDescription(StoreProductDefinition product)
    {
        if (product == null)
            return string.Empty;

        List<string> lines = new List<string>();

        switch (product.rewardType)
        {
            case RewardType.Coins:
                lines.Add($"Includes {product.coinAmount} ninja coins.");
                break;

            case RewardType.Bundle:
                AppendBundleDescription(lines, product.bundleReward);
                break;

            case RewardType.RemoveAds:
                lines.Add("Removes interstitial ads permanently.");
                lines.Add("Rewarded ads remain available.");
                break;
        }

        if (lines.Count == 0)
            lines.Add("Confirms the purchase of this store item.");

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0)
                builder.AppendLine();

            builder.Append("- ");
            builder.Append(lines[i]);
        }

        return builder.ToString();
    }

    private static void AppendBundleDescription(List<string> lines, BundleRewardData reward)
    {
        if (lines == null || reward == null)
            return;

        if (reward.coins > 0)
            lines.Add($"Includes {reward.coins} coins.");

        if (reward.unlimitedLives && reward.unlimitedLivesDurationMinutes > 0f)
            lines.Add($"Includes unlimited lives for {FormatDurationMinutes(reward.unlimitedLivesDurationMinutes)}.");
        else if (reward.regularLivesCount > 0)
            lines.Add($"Includes {reward.regularLivesCount} {(reward.regularLivesCount == 1 ? "life" : "lives")}.");

        if (reward.powerUps == null)
            return;

        foreach (PowerUpRewardEntry entry in reward.powerUps)
        {
            if (entry.quantity <= 0)
                continue;

            lines.Add($"Includes {entry.quantity} {Humanize(entry.type.ToString())}.");
        }
    }

    private static string FormatDurationMinutes(float minutes)
    {
        int roundedMinutes = Mathf.RoundToInt(minutes);
        return roundedMinutes == 1 ? "1 minute" : $"{roundedMinutes} minutes";
    }

    private static string Humanize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string value = raw.Replace("_", " ").Trim();
        StringBuilder builder = new StringBuilder(value.Length + 8);

        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];
            if (i > 0 && char.IsUpper(current) && !char.IsWhiteSpace(value[i - 1]))
                builder.Append(' ');

            builder.Append(current);
        }

        return builder.ToString().Trim();
    }

    private void OnDestroy()
    {
        if (_buyButton != null)
            _buyButton.onClick.RemoveListener(OnBuyClicked);

        if (_cancelButton != null)
            _cancelButton.onClick.RemoveListener(OnCancelClicked);
    }
}
