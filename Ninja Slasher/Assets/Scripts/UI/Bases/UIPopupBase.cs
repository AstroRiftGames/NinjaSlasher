using UnityEngine;
using DG.Tweening;

public abstract class UIPopupBase : UIPanel
{
    [Header("Popup Animation")]
    [SerializeField] protected float _animationDuration = 0.3f;
    [SerializeField] protected Ease _openEase = Ease.OutBack;
    [SerializeField] protected Ease _closeEase = Ease.InBack;
    [SerializeField] protected float _scaleOvershoot = 1.1f;

    protected override bool BlocksUnderlyingUI => true;

    public event System.Action<UIPopupBase> HiddenCompleted;

    protected bool _isOpening;
    protected bool _isClosing;

    public bool IsOpening => _isOpening;
    public bool IsClosing => _isClosing;
    public bool IsTransitioning => _isOpening || _isClosing;
    public bool IsActiveOrTransitioning => _isVisible || _isOpening || _isClosing;

    protected override void Awake()
    {
        base.Awake();

        if (_panelTransform == null)
            _panelTransform = GetComponent<RectTransform>();

        _panelTransform = ResolvePreferredPanelTransform();
    }

    public override void Show()
    {
        if (_isVisible || _isOpening) return;

        gameObject.SetActive(true);
        _isOpening = true;
        _isClosing = false;
        _isVisible = true;

        NotifyPanelShown();
        AnimateShow();

        OnShown();
    }

    public override void Hide()
    {
        if (!_isVisible || _isClosing) return;

        _isOpening = false;
        _isClosing = true;
        _isVisible = false;

        SetPanelInputEnabled(false);
        AnimateHide();

        OnHidden();
    }

    public override void HideImmediate()
    {
        DOTween.Kill(_panelTransform);

        if (!_isVisible && !_isOpening && !_isClosing && !gameObject.activeSelf)
            return;

        bool wasTransitioning = _isVisible || _isOpening || _isClosing;
        _isOpening = false;
        _isClosing = false;
        _isVisible = false;
        SetPanelInputEnabled(false);

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        if (_panelTransform != null)
            _panelTransform.localScale = Vector3.one;

        gameObject.SetActive(false);

        if (wasTransitioning)
            CompleteHide();
    }

    protected virtual void AnimateShow()
    {
        DOTween.Kill(_panelTransform);

        _panelTransform.localScale = Vector3.zero;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        SetPanelInputEnabled(true);

        Sequence showSequence = DOTween.Sequence();
        showSequence.Append(_panelTransform.DOScale(_scaleOvershoot, _animationDuration * 0.7f)
            .SetEase(_openEase));
        showSequence.Append(_panelTransform.DOScale(1f, _animationDuration * 0.3f)
            .SetEase(Ease.InOutQuad));

        showSequence.OnComplete(() =>
        {
            _isOpening = false;
        });
        showSequence.SetUpdate(true);

        if (_audioContext != null && _audioContext.Audio != null)
        {
            AudioService.Instance?.PlaySFX(_audioContext.Audio.panelOpen);
        }
    }

    protected virtual void AnimateHide()
    {
        DOTween.Kill(_panelTransform);

        _panelTransform.DOScale(0f, _animationDuration)
            .SetEase(_closeEase)
            .OnComplete(() =>
            {
                _isClosing = false;
                gameObject.SetActive(false);

                if (_canvasGroup != null)
                    _canvasGroup.alpha = 1f;

                _panelTransform.localScale = Vector3.one;
                CompleteHide();
            })
            .SetUpdate(true);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        DOTween.Kill(_panelTransform);

        bool wasTransitioning = _isVisible || _isOpening || _isClosing;
        _isVisible = false;
        _isOpening = false;
        _isClosing = false;

        if (wasTransitioning)
            CompleteHide();
    }

    private void CompleteHide()
    {
        HiddenCompleted?.Invoke(this);
    }
}
