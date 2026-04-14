using System;
using UnityEngine;
using UnityEngine.UI;

public class RestartConfirmationPopUp : UIPopupBase
{
    [Header("REFERENCES")]
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _cancelButton;

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
        ResetViewState();
        _isInitialized = true;
    }

    private void SetupButtons()
    {
        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveListener(OnConfirmClicked);
            _restartButton.onClick.AddListener(OnConfirmClicked);
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
        if (_restartButton != null)
            _restartButton.onClick.RemoveListener(OnConfirmClicked);

        if (_cancelButton != null)
            _cancelButton.onClick.RemoveListener(OnCancelClicked);
    }
}
