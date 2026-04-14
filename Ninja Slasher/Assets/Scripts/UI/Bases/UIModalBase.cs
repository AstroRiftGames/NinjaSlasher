using System.Collections;
using DG.Tweening;
using UnityEngine;

public abstract class UIModalBase : UIPanel
{
    [Header("Modal Background")]
    [SerializeField] protected bool _hasBackground = true;
    [SerializeField] protected UnityEngine.UI.Image _backgroundImage;

    [Header("Modal Animation")]
    [SerializeField] protected Animator _modalAnimator;
    [SerializeField] protected bool _useCanvasGroupFadeWhenNoAnimator = true;
    [SerializeField] protected float _fadeAnimationDuration = 0.2f;
    [SerializeField] protected Ease _showFadeEase = Ease.OutQuad;
    [SerializeField] protected Ease _hideFadeEase = Ease.InQuad;
    [SerializeField] protected string _openTrigger = "Open";
    [SerializeField] protected string _closeTrigger = "Close";

    protected override bool BlocksUnderlyingUI => true;

    private Tween _canvasGroupTween;
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

        _isVisible = gameObject.activeSelf;
    }

    public override void Show()
    {
        if (_isVisible) return;

        CancelPendingDeactivate();
        KillActiveTween();

        gameObject.SetActive(true);
        _isVisible = true;

        if (_hasBackground && _backgroundImage != null)
        {
            _backgroundImage.raycastTarget = true;
        }

        PlayShowAnimation();
        NotifyPanelShown();
        OnShown();
    }

    public override void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;

        SetPanelInputEnabled(false);

        if (_hasBackground && _backgroundImage != null)
        {
            _backgroundImage.raycastTarget = false;
        }

        OnHidden();
        PlayHideAnimation();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CancelPendingDeactivate();
        KillActiveTween();
    }

    private void PlayShowAnimation()
    {
        if (_modalAnimator != null)
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;

            if (!string.IsNullOrEmpty(_closeTrigger))
                _modalAnimator.ResetTrigger(_closeTrigger);

            if (!string.IsNullOrEmpty(_openTrigger))
                _modalAnimator.SetTrigger(_openTrigger);

            return;
        }

        if (_canvasGroup == null)
            return;

        if (!_useCanvasGroupFadeWhenNoAnimator)
        {
            _canvasGroup.alpha = 1f;
            return;
        }

        _canvasGroup.alpha = 0f;
        _canvasGroupTween = _canvasGroup
            .DOFade(1f, _fadeAnimationDuration)
            .SetEase(_showFadeEase)
            .SetUpdate(true);
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

        if (_canvasGroup == null || !_useCanvasGroupFadeWhenNoAnimator)
        {
            gameObject.SetActive(false);
            return;
        }

        _canvasGroupTween = _canvasGroup
            .DOFade(0f, _fadeAnimationDuration)
            .SetEase(_hideFadeEase)
            .SetUpdate(true)
            .OnComplete(() => gameObject.SetActive(false));
    }

    private IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        _delayedDeactivateCoroutine = null;
        gameObject.SetActive(false);
    }

    private void CancelPendingDeactivate()
    {
        if (_delayedDeactivateCoroutine == null)
            return;

        StopCoroutine(_delayedDeactivateCoroutine);
        _delayedDeactivateCoroutine = null;
    }

    private void KillActiveTween()
    {
        if (_canvasGroupTween == null)
            return;

        _canvasGroupTween.Kill();
        _canvasGroupTween = null;
    }
}
