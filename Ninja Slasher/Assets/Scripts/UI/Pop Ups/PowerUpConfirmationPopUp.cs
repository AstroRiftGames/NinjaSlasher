using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpConfirmationPopUp : UIPopupBase
{
    [Header("REFERENCES")]
    [SerializeField] private Image _purchaseIcon;
    [SerializeField] private TextMeshProUGUI _purchaseNameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Button _buyButton;
    [SerializeField] private Button _cancelButton;

    private Action _confirmAction;
    private bool _isInitialized;

    protected override void Awake()
    {
        base.Awake();
        InitializeIfNeeded();
    }

    public void ShowConfirmation(PowerUpBase powerUp, Action onConfirm)
    {
        InitializeIfNeeded();

        if (powerUp == null)
        {
            Debug.LogWarning("[PowerUpConfirmationPopUp] Cannot show confirmation for a null power-up.");
            return;
        }

        _confirmAction = onConfirm;

        ApplyPowerUp(powerUp);
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

        SetupButtons();
        ResetViewState();
        _isInitialized = true;
    }

    private void ApplyPowerUp(PowerUpBase powerUp)
    {
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
    }

    private void OnDestroy()
    {
        if (_buyButton != null)
            _buyButton.onClick.RemoveListener(OnConfirmClicked);

        if (_cancelButton != null)
            _cancelButton.onClick.RemoveListener(OnCancelClicked);
    }
}
