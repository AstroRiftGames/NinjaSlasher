using System;
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

    public event Action<UIModalBase> HiddenCompleted;

    protected bool _isOpening;
    protected bool _isClosing;

    public bool IsOpening => _isOpening;
    public bool IsClosing => _isClosing;
    public bool IsTransitioning => _isOpening || _isClosing;
    public bool IsActiveOrTransitioning => _isVisible || _isOpening || _isClosing || gameObject.activeInHierarchy;

    private Sequence _contentAnimationSequence;
    private Tween _overlayFadeTween;
    private Coroutine _delayedDeactivateCoroutine;
    private Coroutine _showCompletionCoroutine;
    private bool _isWaitingForHideAnimationEvent;
    private float _overlayTargetGroupAlpha = 1f;
    private float _overlayTargetImageAlpha = 1f;
    private string _overlayTargetAlphaSource = "Fallback";
    protected virtual float ShowAnimationDuration => Mathf.Max(_fadeAnimationDuration, _showScaleDuration);
    protected virtual float HideAnimationDuration => 0.4f;

    protected override void Awake()
    {
        base.Awake();

        if (_panelTransform == null)
            _panelTransform = GetComponent<RectTransform>();

        _panelTransform = ResolvePreferredPanelTransform(_backgroundImage != null ? _backgroundImage.transform : null);

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_modalAnimator == null)
            _modalAnimator = GetComponent<Animator>();

        if (_animatedContentTransform == null || _animatedContentTransform == GetComponent<RectTransform>())
            _animatedContentTransform = ResolvePreferredPanelTransform(_backgroundImage != null ? _backgroundImage.transform : null);

        if (_animatedContentCanvasGroup == null)
            _animatedContentCanvasGroup = ResolveAnimatedContentCanvasGroup();

        ResolveOverlayCanvasGroup();
        NormalizeOverlayRectTransform();
        CacheOverlayTargetAlpha();
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
            _overlayCanvasGroup.interactable = false;
        }
    }

    private void NormalizeOverlayRectTransform()
    {
        if (_backgroundImage == null)
            return;

        RectTransform overlayRect = _backgroundImage.rectTransform;
        if (overlayRect == null)
            return;

        overlayRect.localScale = Vector3.one;
        overlayRect.anchoredPosition = Vector2.zero;
    }

    protected void EnsureOverlayStartsInvisible()
    {
        if (_overlayCanvasGroup != null)
        {
            _overlayCanvasGroup.alpha = 0f;
            _overlayCanvasGroup.blocksRaycasts = false;
            _overlayCanvasGroup.interactable = false;
        }
    }

    protected void EnsureOverlayBlocksRaycasts()
    {
        if (_overlayCanvasGroup != null && _hasBackground)
            _overlayCanvasGroup.blocksRaycasts = true;
    }

public override void Show()
    {
        if (_isVisible || _isOpening) return;

        CancelPendingDeactivate();
        CancelShowCompletion();
        KillActiveAnimation();

        _isOpening = true;
        _isClosing = false;

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
        if (!_isVisible || _isClosing) return;

        CancelShowCompletion();

        _isClosing = true;
        _isOpening = false;
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
        _isWaitingForHideAnimationEvent = false;

        if (!_isVisible && !_isOpening && !_isClosing && !gameObject.activeSelf)
            return;

        bool wasTransitioning = _isOpening || _isClosing || _isVisible;

        _isOpening = false;
        _isClosing = false;
        _isVisible = false;
        SetPanelInputEnabled(false);
        EnsureBackgroundClickable();
        NotifyUIManagerModalHidden();
        OnHidden();
        ResetContentVisualState();
        ResetOverlayState();
        gameObject.SetActive(false);
        OnHideAnimationCompleted();

        if (wasTransitioning)
            HiddenCompleted?.Invoke(this);
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
        _isWaitingForHideAnimationEvent = false;

        bool wasTransitioning = _isOpening || _isClosing || _isVisible;
        _isOpening = false;
        _isClosing = false;

        NotifyUIManagerModalHidden();

        if (wasTransitioning)
            HiddenCompleted?.Invoke(this);
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
        PlayOverlayShowVisual();

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
            CompleteShow();
            OnShowAnimationCompleted();
        });
    }

    private void PlayHideAnimation()
    {
        PlayOverlayHideVisual();

        if (_modalAnimator != null)
        {
            _isWaitingForHideAnimationEvent = true;
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
            CompleteHide();
            OnHideAnimationCompleted();
            return;
        }

        KillOverlayFade();

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
            CompleteHide();
            OnHideAnimationCompleted();
        });
    }

    private IEnumerator DeactivateAfterHideAnimation()
    {
        float timeout = Mathf.Max(0.1f, HideAnimationDuration + 0.75f);
        float elapsed = 0f;

        while (_isWaitingForHideAnimationEvent && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _delayedDeactivateCoroutine = null;

        if (_isWaitingForHideAnimationEvent)
            CompleteHideAfterVisuals();
    }

    private IEnumerator CompleteShowAfterAnimator()
    {
        yield return WaitForAnimatorPlayback(_modalAnimator, ShowAnimationDuration);
        _showCompletionCoroutine = null;
        CompleteShow();
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
        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
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
            _overlayCanvasGroup.interactable = false;
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

    protected void CompleteShow()
    {
        _isOpening = false;
    }

    protected void CompleteHide()
    {
        _isClosing = false;
        HiddenCompleted?.Invoke(this);
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

    public void AnimationEvent_NotifyHideVisualCompleted()
    {
        if (!_isWaitingForHideAnimationEvent)
            return;

        CompleteHideAfterVisuals();
    }

    private void CompleteHideAfterVisuals()
    {
        if (!_isWaitingForHideAnimationEvent)
            return;

        _isWaitingForHideAnimationEvent = false;
        _delayedDeactivateCoroutine = null;
        ResetContentVisualState();
        ResetOverlayState();
        gameObject.SetActive(false);
        CompleteHide();
        OnHideAnimationCompleted();
    }

    private void CacheOverlayTargetAlpha()
    {
        if (_backgroundImage == null)
            return;

        if (_backgroundImage.color.a > 0.001f)
        {
            _overlayTargetImageAlpha = _backgroundImage.color.a;
            _overlayTargetAlphaSource = "ImageColor";
        }
        else
        {
            _overlayTargetImageAlpha = 1f;
        }

        if (_overlayCanvasGroup != null && _overlayCanvasGroup.alpha > 0.001f)
        {
            _overlayTargetGroupAlpha = _overlayCanvasGroup.alpha;
            if (_overlayTargetAlphaSource == "Fallback")
                _overlayTargetAlphaSource = "CanvasGroupInitial";
        }
        else
        {
            _overlayTargetGroupAlpha = 1f;
        }

        Debug.Log($"[OverlayVisual] Initialize panel={name} overlay={_backgroundImage.name} targetImageAlpha={_overlayTargetImageAlpha:F3} targetGroupAlpha={_overlayTargetGroupAlpha:F3} source={_overlayTargetAlphaSource} root={GetComponent<RectTransform>()?.name ?? "None"} animatedTransform={_animatedContentTransform?.name ?? "None"}");
    }

    private void PlayOverlayShowVisual()
    {
        if (!_hasBackground || _backgroundImage == null)
            return;

        KillOverlayFade();
        _backgroundImage.gameObject.SetActive(true);
        NormalizeOverlayRectTransform();

        bool fadeEnabled = _fadeAnimationDuration > 0f;
        float alphaBefore = _overlayCanvasGroup != null ? _overlayCanvasGroup.alpha : _backgroundImage.color.a;
        Color overlayColor = _backgroundImage.color;
        overlayColor.a = _overlayTargetImageAlpha;
        _backgroundImage.color = overlayColor;

        if (_overlayCanvasGroup != null)
        {
            _overlayCanvasGroup.alpha = fadeEnabled ? 0f : _overlayTargetGroupAlpha;
            _overlayCanvasGroup.blocksRaycasts = true;
            _overlayCanvasGroup.interactable = false;
        }

        if (_animatedContentTransform != null && _backgroundImage.transform.IsChildOf(_animatedContentTransform))
        {
            Debug.LogWarning($"[OverlayVisual] OverlayInsideAnimatedTransform panel={name} overlay={_backgroundImage.name} animatedTransform={_animatedContentTransform.name}");
        }

        Debug.Log($"[OverlayVisual] Show panel={name} overlay={_backgroundImage.name} activeSelf={_backgroundImage.gameObject.activeSelf} alphaBefore={alphaBefore:F3} imageAlpha={_backgroundImage.color.a:F3} targetGroupAlpha={_overlayTargetGroupAlpha:F3} blocksRaycasts={_overlayCanvasGroup?.blocksRaycasts ?? false} interactable={_overlayCanvasGroup?.interactable ?? false} fadeEnabled={fadeEnabled} animatedTransform={_animatedContentTransform?.name ?? "None"} panelVisualTransform={_panelTransform?.name ?? "None"} root={GetComponent<RectTransform>()?.name ?? "None"}");

        if (_overlayCanvasGroup != null && fadeEnabled)
        {
            _overlayFadeTween = _overlayCanvasGroup.DOFade(_overlayTargetGroupAlpha, _fadeAnimationDuration * 0.6f)
                .SetEase(_showFadeEase)
                .SetUpdate(true)
                .OnKill(() => { if (_overlayCanvasGroup != null) _overlayCanvasGroup.alpha = _overlayTargetGroupAlpha; });
        }
        else if (_overlayCanvasGroup != null)
        {
            _overlayCanvasGroup.alpha = _overlayTargetGroupAlpha;
        }
    }

    private void PlayOverlayHideVisual()
    {
        if (!_hasBackground || _backgroundImage == null)
            return;

        KillOverlayFade();

        float alphaBefore = _overlayCanvasGroup != null ? _overlayCanvasGroup.alpha : _backgroundImage.color.a;
        bool fadeEnabled = _fadeAnimationDuration > 0f;

        if (_overlayCanvasGroup != null)
        {
            Debug.Log($"[OverlayVisual] Hide panel={name} overlay={_backgroundImage.name} alphaBefore={alphaBefore:F3} targetHideAlpha=0.000 blocksRaycastsBefore={_overlayCanvasGroup.blocksRaycasts} activeBefore={_backgroundImage.gameObject.activeSelf}");

            _overlayCanvasGroup.blocksRaycasts = false;
            _overlayCanvasGroup.interactable = false;

            if (fadeEnabled)
            {
                _overlayFadeTween = _overlayCanvasGroup.DOFade(0f, _fadeAnimationDuration * 0.5f)
                    .SetEase(_hideFadeEase)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        Debug.Log($"[OverlayVisual] HideComplete panel={name} overlay={_backgroundImage.name} alphaAfter={_overlayCanvasGroup.alpha:F3} blocksRaycastsAfter={_overlayCanvasGroup.blocksRaycasts} activeAfter={_backgroundImage.gameObject.activeSelf}");
                    });
            }
            else
            {
                _overlayCanvasGroup.alpha = 0f;
                Debug.Log($"[OverlayVisual] HideComplete panel={name} overlay={_backgroundImage.name} alphaAfter={_overlayCanvasGroup.alpha:F3} blocksRaycastsAfter={_overlayCanvasGroup.blocksRaycasts} activeAfter={_backgroundImage.gameObject.activeSelf}");
            }
        }
    }
}
