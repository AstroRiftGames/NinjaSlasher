using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ConfirmationPopUp : UIPopupBase
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private TextMeshProUGUI _confirmButtonText;
    [SerializeField] private TextMeshProUGUI _cancelButtonText;

    private Action _onConfirmAction;
    private Action _onCancelAction;

    protected override void Awake()
    {
        base.Awake();
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (_confirmButton != null)
            _confirmButton.onClick.AddListener(OnConfirmClicked);

        if (_cancelButton != null)
            _cancelButton.onClick.AddListener(OnCancelClicked);
    }

    public void ShowConfirmation(
        string message,
        Action onConfirm,
        Action onCancel = null,
        string title = "Confirmatin",
        string confirmText = "Confirm",
        string cancelText = "Cancel")
    {
        if (_titleText != null)
            _titleText.text = title;

        if (_messageText != null)
            _messageText.text = message;

        if (_confirmButtonText != null)
            _confirmButtonText.text = confirmText;

        if (_cancelButtonText != null)
            _cancelButtonText.text = cancelText;

        _onConfirmAction = onConfirm;
        _onCancelAction = onCancel;

        Show();
    }

    private void OnConfirmClicked()
    {
        Hide();
        _onConfirmAction?.Invoke();
        ClearCallbacks();
    }

    private void OnCancelClicked()
    {
        Hide();
        _onCancelAction?.Invoke();
        ClearCallbacks();
    }

    private void ClearCallbacks()
    {
        _onConfirmAction = null;
        _onCancelAction = null;
    }

    protected override void OnShown()
    {
        Debug.Log("[ConfirmationPanel] Panel mostrado");
    }

    protected override void OnHidden()
    {
        Debug.Log("[ConfirmationPanel] Panel ocultado");
        ClearCallbacks();
    }

    private void OnDestroy()
    {
        if (_confirmButton != null)
            _confirmButton.onClick.RemoveAllListeners();

        if (_cancelButton != null)
            _cancelButton.onClick.RemoveAllListeners();

        ClearCallbacks();
    }
}