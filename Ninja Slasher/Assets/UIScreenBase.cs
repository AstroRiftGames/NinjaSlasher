using UnityEngine;

public abstract class UIScreenBase : UIPanel
{
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
            _canvasGroup.blocksRaycasts = true;
        }

        OnShown();
    }

    public override void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = false;
        }

        OnHidden();

        gameObject.SetActive(false);
    }
}