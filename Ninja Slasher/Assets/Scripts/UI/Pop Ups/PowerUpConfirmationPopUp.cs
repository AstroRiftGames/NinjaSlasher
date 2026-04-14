using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PowerUpConfirmationRequest
{
    public PowerUpBase PowerUp { get; }
    public string PrimaryButtonLabel { get; }
    public bool IsPrimaryButtonInteractable { get; }
    public Sprite PrimaryButtonIcon { get; }
    public Action PrimaryAction { get; }

    public PowerUpConfirmationRequest(PowerUpBase powerUp, string primaryButtonLabel, bool isPrimaryButtonInteractable, Sprite primaryButtonIcon, Action primaryAction)
    {
        PowerUp = powerUp;
        PrimaryButtonLabel = primaryButtonLabel;
        IsPrimaryButtonInteractable = isPrimaryButtonInteractable;
        PrimaryButtonIcon = primaryButtonIcon;
        PrimaryAction = primaryAction;
    }
}

public class PowerUpConfirmationPopUp : UIPopupBase
{
    [Header("REFERENCES")]
    [SerializeField] private Image _purchaseIcon;
    [SerializeField] private TextMeshProUGUI _purchaseNameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Button _buyButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private TextMeshProUGUI _buyButtonLabel;
    [SerializeField] private Image _buyButtonIcon;

    [Header("LABELS")]
    [SerializeField] private string _activateButtonLabel = "Activar";
    [SerializeField] private string _buyButtonLabelFormat = "{0}";

    private Action _confirmAction;
    private bool _isInitialized;

    protected override void Awake()
    {
        base.Awake();
        InitializeIfNeeded();
    }

    public void ShowConfirmation(PowerUpConfirmationRequest request)
    {
        InitializeIfNeeded();

        if (request == null || request.PowerUp == null)
        {
            Debug.LogWarning("[PowerUpConfirmationPopUp] Cannot show confirmation for a null power-up.");
            return;
        }

        _confirmAction = request.PrimaryAction;

        ApplyRequest(request);
        Show();
    }

    public override void HideImmediate()
    {
        ResetViewState();
        base.HideImmediate();
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
            _buyButton.onClick.RemoveListener(OnConfirmClicked);
            _buyButton.onClick.AddListener(OnConfirmClicked);
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

        ResolveOptionalReferences();
        SetupButtons();
        ResetViewState();
        _isInitialized = true;
    }

    private void ResolveOptionalReferences()
    {
        if (_buyButtonLabel == null && _buyButton != null)
            _buyButtonLabel = _buyButton.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void ApplyRequest(PowerUpConfirmationRequest request)
    {
        PowerUpBase powerUp = request.PowerUp;

        if (_purchaseNameText != null)
        {
            _purchaseNameText.gameObject.SetActive(true);
            _purchaseNameText.text = powerUp.displayName;
        }

        if (_descriptionText != null)
        {
            _descriptionText.gameObject.SetActive(true);
            _descriptionText.text = powerUp.description;
        }

        if (_purchaseIcon != null)
        {
            _purchaseIcon.gameObject.SetActive(true);
            _purchaseIcon.sprite = powerUp.icon;
            _purchaseIcon.enabled = powerUp.icon != null;
        }

        if (_buyButton != null)
            _buyButton.interactable = request.IsPrimaryButtonInteractable;

        if (_buyButtonLabel != null)
            _buyButtonLabel.text = request.PrimaryButtonLabel;

        if (_buyButtonIcon != null)
        {
            bool showIcon = request.PrimaryButtonIcon != null;
            _buyButtonIcon.gameObject.SetActive(showIcon);
            _buyButtonIcon.sprite = request.PrimaryButtonIcon;
            _buyButtonIcon.enabled = showIcon;
        }
    }

    private void OnConfirmClicked()
    {
        Action confirmAction = _confirmAction;

        Hide();
        confirmAction?.Invoke();
    }

    private void OnCancelClicked()
    {
        Hide();
    }

    private void ResetViewState()
    {
        _confirmAction = null;

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
            _buyButton.interactable = true;

        if (_buyButtonLabel != null)
            _buyButtonLabel.text = string.Empty;

        if (_buyButtonIcon != null)
        {
            _buyButtonIcon.gameObject.SetActive(false);
            _buyButtonIcon.sprite = null;
            _buyButtonIcon.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (_buyButton != null)
            _buyButton.onClick.RemoveListener(OnConfirmClicked);

        if (_cancelButton != null)
            _cancelButton.onClick.RemoveListener(OnCancelClicked);
    }

    public string GetActivateButtonLabel() => _activateButtonLabel;

    public string GetBuyButtonLabel(int cost) => string.Format(_buyButtonLabelFormat, cost);
}
