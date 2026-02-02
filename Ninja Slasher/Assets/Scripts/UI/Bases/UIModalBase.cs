using UnityEngine;

public abstract class UIModalBase : UIPanel
{
    [Header("Modal Background")]
    [SerializeField] protected bool _hasBackground = true;
    [SerializeField] protected UnityEngine.UI.Image _backgroundImage;

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

        if (_hasBackground && _backgroundImage != null)
        {
            _backgroundImage.raycastTarget = true;
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

        if (_hasBackground && _backgroundImage != null)
        {
            _backgroundImage.raycastTarget = false;
        }

        OnHidden();

        gameObject.SetActive(false);
    }
}