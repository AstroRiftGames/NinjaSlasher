using UnityEngine;

public abstract class UIPanel : MonoBehaviour
{
    [SerializeField] protected CanvasGroup _canvasGroup;
    [SerializeField] protected RectTransform _panelTransform;

    protected bool _isVisible = false;

    protected UIAudioContext _audioContext;

    protected virtual void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_panelTransform == null)
            _panelTransform = GetComponent<RectTransform>();

        _audioContext = GetComponentInParent<UIAudioContext>();
    }

    public virtual void Show()
    {
        if (_isVisible) return;

        gameObject.SetActive(true);
        _isVisible = true;

        OnShown();
    }

    public virtual void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;

        OnHidden();

        gameObject.SetActive(false);
    }

    protected virtual void OnShown()
    {
    }

    protected virtual void OnHidden()
    {
    }

    public bool IsVisible => _isVisible;
}