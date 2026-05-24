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
    [SerializeField] protected CanvasGroup _overlayCanvasGroup;
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
    private Tween _overlayFadeTween;
    private Coroutine _delayedDeactivateCoroutine;
    private Coroutine _showCompletionCoroutine;
    protected virtual float ShowAnimationDuration => Mathf.Max(_fadeAnimationDuration, _showScaleDuration);
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

        ResolveOverlayCanvasGroup();
        EnsureBackgroundClickable();
        _isVisible = gameObject.activeSelf;
    }

    private void ResolveOverlayCanvasGroup()
    {
        if (_overlayCanvasGroup != null)
            return;

        if (_backgroundImage != null)
        {
            _overlayCanvasGroup = _backgroundImage.GetComponent<CanvasGroup>();
            if (_overlayCanvasGroup == null)
                _overlayCanvasGroup = _backgroundImage.gameObject.AddComponent<CanvasGroup>();

            _overlayCanvasGroup.blocksRaycasts = true;
        }
    }

    protected void EnsureOverlayStartsInvisible()
    {
        if (_overlayCanvasGroup != null)
        {
            _overlayCanvasGroup.alpha = 0f;
            _overlayCanvasGroup.blocksRaycasts = false;
        }
    }

    protected void EnsureOverlayBlocksRaycasts()
    {
        if (_overlayCanvasGroup != null && _hasBackground)
            _overlayCanvasGroup.blocksRaycasts = true;
    }

public override void Show()
    {
        if (_isVisible) return;

        CancelPendingDeactivate();
        CancelShowCompletion();
        KillActiveAnimation();

        gameObject.SetActive(true);
        _isVisible = true;

        EnsureOverlayStartsInvisible();
        EnsureBackgroundClickable();
        PlayShowAnimation();
        NotifyPanelShown();
        NotifyUIManagerModalShown();
        OnShown();
    }

    public override void Hide()
    {
        if (!_isVisible) return;

        CancelShowCompletion();
        _isVisible = false;
        SetPanelInputEnabled(false);

        EnsureBackgroundClickable();
        NotifyUIManagerModalHidden();
        OnHidden();
        PlayHideAnimation();
    }

public override void HideImmediate()
    {
        CancelPendingDeactivate();
        CancelShowCompletion();
        KillActiveAnimation();

        if (!_isVisible && !gameObject.activeSelf)
            return;

        _isVisible = false;
        SetPanelInputEnabled(false);
        EnsureBackgroundClickable();
        NotifyUIManagerModalHidden();
        OnHidden();
        ResetContentVisualState();
        ResetOverlayState();
        gameObject.SetActive(false);
        OnHideAnimationCompleted();
    }

    public override IEnumerator ShowRoutine()
    {
        if (_isVisible)
            yield break;

        Show();
        yield return WaitForShowAnimationToFinish();
    }

    public override IEnumerator HideRoutine()
    {
        if (!_isVisible)
            yield break;

        Hide();
        yield return WaitForHideAnimationToFinish();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CancelPendingDeactivate();
        CancelShowCompletion();
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
        ResetContentVisualState();

        if (_modalAnimator != null)
        {
            if (!string.IsNullOrEmpty(_closeTrigger))
                _modalAnimator.ResetTrigger(_closeTrigger);
            if (!string.IsNullOrEmpty(_openTrigger))
                _modalAnimator.SetTrigger(_openTrigger);
            _showCompletionCoroutine = StartCoroutine(CompleteShowAfterAnimator());
            return;
        }

        if (!_useCanvasGroupFadeWhenNoAnimator && !_useContentScaleWhenNoAnimator)
        {
            OnShowAnimationCompleted();
            return;
        }

        KillOverlayFade();

        if (_overlayCanvasGroup != null && _hasBackground)
        {
            _overlayCanvasGroup.alpha = 0f;
            _overlayCanvasGroup.blocksRaycasts = true;
            _overlayFadeTween = _overlayCanvasGroup.DOFade(1f, _fadeAnimationDuration * 0.6f)
                .SetEase(_showFadeEase)
                .SetUpdate(true);
        }

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

        _contentAnimationSequence.OnComplete(() =>
        {
            _contentAnimationSequence = null;
OnShowAnimationCompleted();
        });
    }

    private void PlayHideAnimation()
    {
        if (_modalAnimator != null)
        {
            if (!string.IsNullOrEmpty(_openTrigger))
                _modalAnimator.ResetTrigger(_openTrigger);
            if (!string.IsNullOrEmpty(_closeTrigger))
                _modalAnimator.SetTrigger(_closeTrigger);
            _delayedDeactivateCoroutine = StartCoroutine(DeactivateAfterHideAnimation());
            return;
        }

        if (!_useCanvasGroupFadeWhenNoAnimator && !_useContentScaleWhenNoAnimator)
        {
            ResetContentVisualState();
            ResetOverlayState();
            gameObject.SetActive(false);
            OnHideAnimationCompleted();
            return;
        }

        KillOverlayFade();

        if (_overlayCanvasGroup != null && _hasBackground)
        {
            _overlayFadeTween = _overlayCanvasGroup.DOFade(0f, _fadeAnimationDuration * 0.5f)
                .SetEase(_hideFadeEase)
                .SetUpdate(true);
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
            _contentAnimationSequence = null;
            ResetContentVisualState();
            ResetOverlayState();
            gameObject.SetActive(false);
            OnHideAnimationCompleted();
        });
    }

    private IEnumerator DeactivateAfterHideAnimation()
    {
        yield return WaitForAnimatorPlayback(_modalAnimator, HideAnimationDuration);
        _delayedDeactivateCoroutine = null;
        ResetContentVisualState();
        gameObject.SetActive(false);
        OnHideAnimationCompleted();
    }

    private IEnumerator CompleteShowAfterAnimator()
    {
        yield return WaitForAnimatorPlayback(_modalAnimator, ShowAnimationDuration);
        _showCompletionCoroutine = null;
        OnShowAnimationCompleted();
    }

    private void CancelPendingDeactivate()
    {
        if (_delayedDeactivateCoroutine == null) return;
        StopCoroutine(_delayedDeactivateCoroutine);
        _delayedDeactivateCoroutine = null;
    }

    private void CancelShowCompletion()
    {
        if (_showCompletionCoroutine == null) return;
        StopCoroutine(_showCompletionCoroutine);
        _showCompletionCoroutine = null;
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

    protected void ResetOverlayState()
    {
        if (_overlayCanvasGroup != null)
        {
            _overlayCanvasGroup.alpha = 0f;
            _overlayCanvasGroup.blocksRaycasts = false;
        }
    }

    private void KillActiveAnimation()
    {
        if (_contentAnimationSequence != null)
        {
            _contentAnimationSequence.Kill();
            _contentAnimationSequence = null;
        }
        KillOverlayFade();
    }

    private void KillOverlayFade()
    {
        if (_overlayFadeTween != null)
        {
            _overlayFadeTween.Kill();
            _overlayFadeTween = null;
        }
    }

    private IEnumerator WaitForShowAnimationToFinish()
    {
        if (_modalAnimator != null)
        {
            yield return WaitForAnimatorPlayback(_modalAnimator, ShowAnimationDuration);
            yield break;
        }

        while (_contentAnimationSequence != null && _contentAnimationSequence.IsActive())
        {
            yield return null;
        }
    }

    private IEnumerator WaitForHideAnimationToFinish()
    {
        if (_modalAnimator != null)
        {
            float timeout = Mathf.Max(0.1f, HideAnimationDuration + 0.75f);
            yield return WaitForGameObjectToDeactivate(gameObject, timeout);
            yield break;
        }

        while (_contentAnimationSequence != null && _contentAnimationSequence.IsActive())
        {
            yield return null;
        }
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
