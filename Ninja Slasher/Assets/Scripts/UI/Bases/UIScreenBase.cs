using UnityEngine;

public abstract class UIScreenBase : UIPanel
{
    protected override bool BlocksUnderlyingUI => true;

    protected override void Awake()
    {
        base.Awake();

        if (_panelTransform == null)
            _panelTransform = GetComponent<RectTransform>();

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        _isVisible = gameObject.activeSelf;
    }

    public override void Show()
    {
        if (_isVisible) return;

        gameObject.SetActive(true);
        _isVisible = true;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }

        NotifyPanelShown();
        OnShown();
    }

    public override void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;

        SetPanelInputEnabled(false);

        OnHidden();

        gameObject.SetActive(false);
    }
}
