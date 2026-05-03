using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class UIModalBase : UIPanel, IPointerClickHandler
{
    [Header("Modal Background")]
    [SerializeField] protected bool _hasBackground = true;
    [SerializeField] protected Image _backgroundImage;
    [SerializeField] protected bool _closeOnOutsideClick = false;

    [Header("Modal Animation")]
    [SerializeField] protected Animator _modalAnimator;
    [SerializeField] protected bool _useCanvasGroupFadeWhenNoAnimator = true;
    [SerializeField] protected bool _useContentScaleWhenNoAnimator = true;
    [SerializeField] protected RectTransform _animatedContentTransform;
    [SerializeField] protected CanvasGroup _animatedContentCanvasGroup;
    [SerializeField] protected float _fadeAnimationDuration = 0.2f;
    [SerializeField] protected Ease _showFadeEase = Ease.OutQuad;
    [SerializeField] protected Ease _hideFadeEase = Ease.InQuad;
    [SerializeField] protected float _showScaleDuration = 0.22f;
    [SerializeField] protected float _hideScaleDuration = 0.18f;
    [SerializeField] protected Ease _showScaleEase = Ease.OutBack;
    [SerializeField] protected Ease _hideScaleEase = Ease.InBack;
    [SerializeField] protected float _hiddenScaleMultiplier = 0.94f;
    [SerializeField] protected string _openTrigger = "Open";
    [SerializeField] protected string _closeTrigger = "Close";

    protected override bool BlocksUnderlyingUI => true;

    private Sequence _contentAnimationSequence;
    private Coroutine _delayedDeactivateCoroutine;
    protected virtual float HideAnimationDuration => 0.4f;

    protected override void Awake()
    {
        base.Awake();

        if (_panelTransform == null)
            _panelTransform = GetComponent<RectTransform>();

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_modalAnimator == null)
            _modalAnimator = GetComponent<Animator>();

        if (_animatedContentTransform == null)
            _animatedContentTransform = _panelTransform;

        if (_animatedContentCanvasGroup == null)
            _animatedContentCanvasGroup = ResolveAnimatedContentCanvasGroup();

        EnsureBackgroundClickable();
        _isVisible = gameObject.activeSelf;
    }

    public override void Show()
    {
        if (_isVisible) return;

        CancelPendingDeactivate();
        KillActiveAnimation();

        gameObject.SetActive(true);
        _isVisible = true;

        EnsureBackgroundClickable();
        PlayShowAnimation();
        NotifyPanelShown();
        NotifyUIManagerModalShown();
        OnShown();
    }

    public override void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;
        SetPanelInputEnabled(false);

        EnsureBackgroundClickable();
        NotifyUIManagerModalHidden();
        OnHidden();
        PlayHideAnimation();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CancelPendingDeactivate();
        KillActiveAnimation();
        NotifyUIManagerModalHidden();
    }

    protected void SetBackgroundRaycastTarget(bool enabled)
    {
        if (_hasBackground && _backgroundImage != null)
            _backgroundImage.raycastTarget = enabled;
    }

    protected void NotifyUIManagerModalShown() => UIManager.Instance?.NotifyModalShown(this);
    protected void NotifyUIManagerModalHidden() => UIManager.Instance?.NotifyModalHidden(this);

    public void OnPointerClick(PointerEventData eventData) => HandlePointerClick(eventData);

    internal void HandlePointerClick(PointerEventData eventData)
    {
        if (!_closeOnOutsideClick || !_isVisible || eventData == null)
            return;

        if (WasOutsideSurfaceClicked(eventData))
            RequestCloseFromOutsideClick();
    }

    private void PlayShowAnimation()
    {
        if (_modalAnimator != null)
        {
            ResetContentVisualState();
            if (!string.IsNullOrEmpty(_closeTrigger))
                _modalAnimator.ResetTrigger(_closeTrigger);
            if (!string.IsNullOrEmpty(_openTrigger))
                _modalAnimator.SetTrigger(_openTrigger);
            return;
        }

        ResetContentVisualState();

        if (!_useCanvasGroupFadeWhenNoAnimator && !_useContentScaleWhenNoAnimator)
            return;

        _contentAnimationSequence = DOTween.Sequence().SetUpdate(true);

        if (_animatedContentCanvasGroup != null && _useCanvasGroupFadeWhenNoAnimator)
        {
            _animatedContentCanvasGroup.alpha = 0f;
            _contentAnimationSequence.Join(
                _animatedContentCanvasGroup.DOFade(1f, _fadeAnimationDuration)
                    .SetEase(_showFadeEase));
        }

        if (_animatedContentTransform != null && _useContentScaleWhenNoAnimator)
        {
            _animatedContentTransform.localScale = Vector3.one * _hiddenScaleMultiplier;
            _contentAnimationSequence.Join(
                _animatedContentTransform.DOScale(1f, _showScaleDuration)
                    .SetEase(_showScaleEase));
        }
    }

    private void PlayHideAnimation()
    {
        if (_modalAnimator != null)
        {
            if (!string.IsNullOrEmpty(_openTrigger))
                _modalAnimator.ResetTrigger(_openTrigger);
            if (!string.IsNullOrEmpty(_closeTrigger))
                _modalAnimator.SetTrigger(_closeTrigger);
            _delayedDeactivateCoroutine = StartCoroutine(DeactivateAfterDelay(HideAnimationDuration));
            return;
        }

        if (!_useCanvasGroupFadeWhenNoAnimator && !_useContentScaleWhenNoAnimator)
        {
            ResetContentVisualState();
            gameObject.SetActive(false);
            return;
        }

        _contentAnimationSequence = DOTween.Sequence().SetUpdate(true);

        if (_animatedContentCanvasGroup != null && _useCanvasGroupFadeWhenNoAnimator)
        {
            _contentAnimationSequence.Join(
                _animatedContentCanvasGroup.DOFade(0f, _fadeAnimationDuration)
                    .SetEase(_hideFadeEase));
        }

        if (_animatedContentTransform != null && _useContentScaleWhenNoAnimator)
        {
            _contentAnimationSequence.Join(
                _animatedContentTransform.DOScale(_hiddenScaleMultiplier, _hideScaleDuration)
                    .SetEase(_hideScaleEase));
        }

        _contentAnimationSequence.OnComplete(() =>
        {
            ResetContentVisualState();
            gameObject.SetActive(false);
        });
    }

    private IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        _delayedDeactivateCoroutine = null;
        gameObject.SetActive(false);
    }

    private void CancelPendingDeactivate()
    {
        if (_delayedDeactivateCoroutine == null) return;
        StopCoroutine(_delayedDeactivateCoroutine);
        _delayedDeactivateCoroutine = null;
    }

    private CanvasGroup ResolveAnimatedContentCanvasGroup()
    {
        if (_animatedContentTransform == null)
            return _canvasGroup;

        if (_animatedContentTransform == _panelTransform)
            return _canvasGroup;

        CanvasGroup contentCanvasGroup = _animatedContentTransform.GetComponent<CanvasGroup>();
        return contentCanvasGroup != null ? contentCanvasGroup : _animatedContentTransform.gameObject.AddComponent<CanvasGroup>();
    }

    private void ResetContentVisualState()
    {
        if (_animatedContentCanvasGroup != null)
            _animatedContentCanvasGroup.alpha = 1f;
        if (_animatedContentTransform != null)
            _animatedContentTransform.localScale = Vector3.one;
    }

    private void KillActiveAnimation()
    {
        if (_contentAnimationSequence == null) return;
        _contentAnimationSequence.Kill();
        _contentAnimationSequence = null;
    }

    protected virtual void RequestCloseFromOutsideClick()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseModal(this);
            return;
        }
        Hide();
    }

    private bool WasOutsideSurfaceClicked(PointerEventData eventData)
    {
        if (_backgroundImage == null)
            return false;

        GameObject clickedObject = eventData.pointerPressRaycast.gameObject;
        if (clickedObject == null)
            clickedObject = eventData.pointerCurrentRaycast.gameObject;

        if (clickedObject == null)
            return false;

        Transform clickedTransform = clickedObject.transform;
        Transform backgroundTransform = _backgroundImage.transform;
        return clickedTransform == backgroundTransform || clickedTransform.IsChildOf(backgroundTransform);
    }

    private void EnsureBackgroundClickable()
    {
        if (_backgroundImage == null) return;
        _backgroundImage.raycastTarget = _closeOnOutsideClick && _isVisible;
    }
}
