using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public abstract class UIOverlayBase : UIPanel
{
    [Header("Overlay Settings")]
    [SerializeField] protected Image _backgroundImage;
    [SerializeField] protected Color _backgroundColor = new Color(0, 0, 0, 0.7f);
    [SerializeField] protected bool _blockRaycastsWhenHidden = false;

    [Header("Fade Animation")]
    [SerializeField] protected float _backgroundFadeDuration = 0.2f;
    [SerializeField] protected float _contentFadeDuration = 0.3f;
    [SerializeField] protected Ease _fadeEase = Ease.OutQuad;
    [SerializeField] protected bool _useContentScaleAnimation = false;
    [SerializeField] protected float _contentShowScaleDuration = 0.22f;
    [SerializeField] protected float _contentHideScaleDuration = 0.18f;
    [SerializeField] protected Ease _contentShowScaleEase = Ease.OutBack;
    [SerializeField] protected Ease _contentHideScaleEase = Ease.InBack;
    [SerializeField] protected float _hiddenContentScaleMultiplier = 0.94f;

    [Header("Animator Settings")]
    [SerializeField] protected Animator _panelAnimator;
    [SerializeField] protected float _animatorOpenDuration = 0.35f;
    [SerializeField] protected float _animatorCloseDuration = 0.35f;

    protected override bool BlocksUnderlyingUI => true;

    protected override void Awake()
    {
        base.Awake();

        if (_panelTransform == null)
            _panelTransform = GetComponent<RectTransform>();

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_backgroundImage != null)
        {
            _backgroundImage.color = new Color(
                _backgroundColor.r,
                _backgroundColor.g,
                _backgroundColor.b,
                0f
            );
        }
    }

    public override void Show()
    {
        if (_isVisible) return;

        gameObject.SetActive(true);
        _isVisible = true;

        NotifyPanelShown();
        AnimateShow();

        OnShown();
    }

    public override void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;

        AnimateHide();

        OnHidden();
    }

    protected virtual void AnimateShow()
    {
        DOTween.Kill(_backgroundImage);
        DOTween.Kill(_canvasGroup);
        DOTween.Kill(_panelTransform);

        float maxFadeDuration = Mathf.Max(_backgroundFadeDuration, _contentFadeDuration);
        if (_useContentScaleAnimation)
            maxFadeDuration = Mathf.Max(maxFadeDuration, _contentShowScaleDuration);

        Sequence showSequence = DOTween.Sequence();

        if (_backgroundImage != null)
        {
            showSequence.Insert(0f, _backgroundImage.DOFade(_backgroundColor.a, _backgroundFadeDuration)
                .SetEase(_fadeEase));
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            showSequence.Insert(0f, _canvasGroup.DOFade(1f, _contentFadeDuration)
                .SetEase(_fadeEase));
        }

        if (_panelTransform != null && _useContentScaleAnimation)
        {
            _panelTransform.localScale = Vector3.one * _hiddenContentScaleMultiplier;
            showSequence.Insert(0f, _panelTransform.DOScale(1f, _contentShowScaleDuration)
                .SetEase(_contentShowScaleEase));
        }

        if (_panelAnimator != null)
        {
            showSequence.InsertCallback(maxFadeDuration, () =>
            {
                _panelAnimator.SetTrigger("Open");
            });
        }

        showSequence.SetUpdate(true);
    }

    protected virtual void AnimateHide()
    {
        DOTween.Kill(_backgroundImage);
        DOTween.Kill(_canvasGroup);
        DOTween.Kill(_panelTransform);

        // Deshabilitar raycasts al inicio del cierre, no al final de la animación
        if (!_blockRaycastsWhenHidden)
            SetPanelInputEnabled(false);

        Sequence hideSequence = DOTween.Sequence();

        float currentTime = 0f;

        if (_panelAnimator != null)
        {
            hideSequence.InsertCallback(currentTime, () =>
            {
                _panelAnimator.SetTrigger("Close");
            });

            currentTime += _animatorCloseDuration;
        }

        if (_canvasGroup != null)
        {
            hideSequence.Insert(currentTime, _canvasGroup.DOFade(0f, _contentFadeDuration)
                .SetEase(Ease.InQuad));
        }

        if (_panelTransform != null && _useContentScaleAnimation)
        {
            hideSequence.Insert(currentTime, _panelTransform.DOScale(_hiddenContentScaleMultiplier, _contentHideScaleDuration)
                .SetEase(_contentHideScaleEase));
        }

        if (_backgroundImage != null)
        {
            hideSequence.Insert(currentTime, _backgroundImage.DOFade(0f, _backgroundFadeDuration)
                .SetEase(Ease.InQuad));
        }

        hideSequence.OnComplete(() =>
        {
            gameObject.SetActive(false);

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;

            if (_panelTransform != null)
                _panelTransform.localScale = Vector3.one;

            if (_backgroundImage != null)
            {
                _backgroundImage.color = new Color(
                    _backgroundColor.r,
                    _backgroundColor.g,
                    _backgroundColor.b,
                    0f
                );
            }
        });

        hideSequence.SetUpdate(true);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        DOTween.Kill(_backgroundImage);
        DOTween.Kill(_canvasGroup);
        DOTween.Kill(_panelTransform);

        if (_panelTransform != null)
            _panelTransform.localScale = Vector3.one;

        if (!_blockRaycastsWhenHidden)
            SetPanelInputEnabled(false);
    }
}
