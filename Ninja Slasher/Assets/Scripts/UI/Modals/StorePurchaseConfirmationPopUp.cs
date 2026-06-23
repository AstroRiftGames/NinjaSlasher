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
    private bool _isResultMode;
    private bool _playSuccessAudioOnConfirm;
    private TextMeshProUGUI _buyButtonLabel;
    private TextMeshProUGUI _cancelButtonLabel;
    private string _defaultBuyButtonText;
    private string _defaultCancelButtonText;
    private StorePurchaseResultRequest _currentResultRequest;
    private StorePurchaseResultRequest _activeResultDismissRequest;
    private bool _hasCompletedActiveResultDismissal;

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
        _currentResultRequest = null;
        _activeResultDismissRequest = null;
        _hasCompletedActiveResultDismissal = false;

        ApplyProduct(product);
        Show();
    }

    public void ShowResult(StorePurchaseResultRequest request)
    {
        InitializeIfNeeded();

        if (request == null)
            return;

        _isResultMode = true;
        _playSuccessAudioOnConfirm = request.PlaySuccessAudio;
        _currentResultRequest = request;
        _activeResultDismissRequest = request;
        _hasCompletedActiveResultDismissal = false;

        ApplyResult(request);
        Show();
    }

    public override void HideImmediate()
    {
        CompleteDismissalOnce();
        ResetViewState();
        base.HideImmediate();
    }

    protected override void OnHidden()
    {
        CompleteDismissalOnce();
        ResetViewState();
        base.OnHidden();
    }

    public bool TryHandleBack()
    {
        if (!IsVisible)
            return false;

        Hide();
        return true;
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
        if (_audioContext == null)
            _audioContext = GetComponentInParent<UIAudioContext>();
        CacheButtonLabels();
        ResetViewState();
        _isInitialized = true;
    }

    private void CacheButtonLabels()
    {
        _buyButtonLabel ??= _buyButton != null ? _buyButton.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        _cancelButtonLabel ??= _cancelButton != null ? _cancelButton.GetComponentInChildren<TextMeshProUGUI>(true) : null;

        if (_buyButtonLabel != null && string.IsNullOrEmpty(_defaultBuyButtonText))
            _defaultBuyButtonText = _buyButtonLabel.text;

        if (_cancelButtonLabel != null && string.IsNullOrEmpty(_defaultCancelButtonText))
            _defaultCancelButtonText = _cancelButtonLabel.text;
    }

    private void ApplyProduct(StoreProductDefinition product)
    {
        _isResultMode = false;
        _playSuccessAudioOnConfirm = false;

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

        if (_buyButton != null)
            _buyButton.gameObject.SetActive(true);

        if (_cancelButton != null)
            _cancelButton.gameObject.SetActive(true);

        if (_buyButtonLabel != null)
            _buyButtonLabel.text = _defaultBuyButtonText;

        if (_cancelButtonLabel != null)
            _cancelButtonLabel.text = _defaultCancelButtonText;
    }

    private void ApplyResult(StorePurchaseResultRequest request)
    {
        if (_purchaseNameText != null)
        {
            _purchaseNameText.gameObject.SetActive(true);
            _purchaseNameText.text = request.Title ?? string.Empty;
        }

        if (_descriptionText != null)
        {
            _descriptionText.gameObject.SetActive(true);
            _descriptionText.text = request.Message ?? string.Empty;
        }

        if (_purchaseIcon != null)
        {
            _purchaseIcon.gameObject.SetActive(request.Icon != null);
            _purchaseIcon.sprite = request.Icon;
            _purchaseIcon.enabled = request.Icon != null;
        }

        if (_buyButton != null)
        {
            _buyButton.gameObject.SetActive(true);
            if (_buyButtonLabel != null)
                _buyButtonLabel.text = string.IsNullOrWhiteSpace(request.ConfirmButtonText) ? "Aceptar" : request.ConfirmButtonText;
        }

        if (_cancelButton != null)
            _cancelButton.gameObject.SetActive(false);
    }

    private void OnBuyClicked()
    {
        if (_isResultMode)
        {
            if (_playSuccessAudioOnConfirm && _audioContext?.Audio?.powerUp != null)
                AudioService.Instance?.PlaySFX(_audioContext.Audio.powerUp);

            Hide();
            return;
        }

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
        _isResultMode = false;
        _playSuccessAudioOnConfirm = false;
        _currentResultRequest = null;

        if (_purchaseNameText != null)
            _purchaseNameText.text = string.Empty;

        if (_descriptionText != null)
            _descriptionText.text = string.Empty;

        if (_purchaseIcon != null)
        {
            _purchaseIcon.sprite = null;
            _purchaseIcon.enabled = false;
        }

        if (_buyButton != null)
            _buyButton.gameObject.SetActive(true);

        if (_cancelButton != null)
            _cancelButton.gameObject.SetActive(true);
    }

    private static void RaiseResultDismissedIfNeeded(StorePurchaseResultRequest request)
    {
        if (request == null)
            return;

        UIEvents.RaiseStorePurchaseResultDismissed(request);
    }

    private void CompleteDismissalOnce()
    {
        if (_hasCompletedActiveResultDismissal || _activeResultDismissRequest == null)
            return;

        StorePurchaseResultRequest dismissedRequest = _activeResultDismissRequest;
        _hasCompletedActiveResultDismissal = true;
        _activeResultDismissRequest = null;
        RaiseResultDismissedIfNeeded(dismissedRequest);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _isVisible = false;
        SetPanelInputEnabled(false);
        CompleteDismissalOnce();
        ResetViewState();
    }

    private void OnDestroy()
    {
        if (_buyButton != null)
            _buyButton.onClick.RemoveListener(OnBuyClicked);

        if (_cancelButton != null)
            _cancelButton.onClick.RemoveListener(OnCancelClicked);
    }
}
