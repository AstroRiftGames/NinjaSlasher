using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BackToLevelSelectionConfirmationPopUp : UIPopupBase
{
    [Header("REFERENCES")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private TextMeshProUGUI _confirmButtonLabel;

    private Action _confirmAction;
    private bool _isInitialized;

    protected override void Awake()
    {
        base.Awake();
        InitializeIfNeeded();
    }

    public void ShowConfirmation(Action confirmAction)
    {
        InitializeIfNeeded();

        _confirmAction = confirmAction;
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

    private void InitializeIfNeeded()
    {
        if (_isInitialized)
            return;

        SetupButtons();
        _isInitialized = true;
    }

    private void SetupButtons()
    {
        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveListener(OnConfirmClicked);
            _confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.RemoveListener(OnCancelClicked);
            _cancelButton.onClick.AddListener(OnCancelClicked);
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
    }

    private void OnDestroy()
    {
        if (_confirmButton != null)
            _confirmButton.onClick.RemoveListener(OnConfirmClicked);

        if (_cancelButton != null)
            _cancelButton.onClick.RemoveListener(OnCancelClicked);
    }
}
