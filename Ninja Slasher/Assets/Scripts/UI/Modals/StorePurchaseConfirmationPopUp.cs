using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StorePurchaseConfirmationPopUp : UIPopupBase
{
    [Header("REFERENCES")]
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

        SetupButtons();
        ResetViewState();
        _isInitialized = true;
    }

    private void ApplyProduct(StoreProductDefinition product)
    {
        if (_purchaseNameText != null)
        {
            _purchaseNameText.gameObject.SetActive(true);
            _purchaseNameText.text = product.ConfirmationTitle;
        }

        if (_descriptionText != null)
        {
            _descriptionText.gameObject.SetActive(true);
            _descriptionText.text = product.ConfirmationDescription;
        }

        if (_purchaseIcon != null)
        {
            Sprite icon = product.ConfirmationIcon;
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

    private void OnDestroy()
    {
        if (_buyButton != null)
            _buyButton.onClick.RemoveListener(OnBuyClicked);

        if (_cancelButton != null)
            _cancelButton.onClick.RemoveListener(OnCancelClicked);
    }
}
