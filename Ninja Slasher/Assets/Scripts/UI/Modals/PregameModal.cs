using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class PregameModal : UIModalBase
{
    [Header("Tween Animation")]
    [FormerlySerializedAs("_closeAnimationDuration")]
    [SerializeField] private float _animationDuration = 0.4f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InCubic;
    [SerializeField] private Vector2 _hiddenOffset = new(0f, -80f);
    [SerializeField] private float _hiddenScale = 0.92f;
    [SerializeField] [Range(0f, 1f)] private float _hiddenAlpha = 0f;

    [Header("Managers")]
    [SerializeField] private PreGameUIManager _preGameUIManager;
    [SerializeField] private ButtonManager _buttonManager;


    private Tween _moveTween;
    private Tween _scaleTween;
    private Tween _fadeTween;
    private Tween _overlayFadeTween;
    private Vector2 _shownAnchoredPosition;
    private Vector3 _shownScale;

    protected override void Awake()
    {
        base.Awake();

        if (_preGameUIManager == null)
        {
            _preGameUIManager = UIManager.Instance?.GetComponent<PreGameUIManager>();
        }

        if (_buttonManager == null)
        {
            _buttonManager = UIManager.Instance?.GetComponent<ButtonManager>();
        }

        if (_panelTransform == null)
        {
            _panelTransform = GetComponentInChildren<RectTransform>(true);
        }

        if (_panelTransform != null)
        {
            _shownAnchoredPosition = _panelTransform.anchoredPosition;
            _shownScale = _panelTransform.localScale;
        }
    }

public override void Show()
    {
        if (_isVisible) return;

        KillActiveTweens();

        gameObject.SetActive(true);
        _isVisible = true;
        SetBackgroundRaycastTarget(true);

        if (_buttonManager != null)
        {
            _buttonManager.StopAllButtonAnimations();
        }

        if (_overlayCanvasGroup != null)
        {
            _overlayCanvasGroup.alpha = 0f;
            _overlayCanvasGroup.blocksRaycasts = true;
            _overlayFadeTween = _overlayCanvasGroup.DOFade(1f, _animationDuration * 0.6f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        ApplyHiddenState();
        AnimateToShownState();
    }

    public override void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;

        SetPanelInputEnabled(false);
        KillActiveTweens();

        NotifyUIManagerModalHidden();
        OnHidden();

        if (_overlayCanvasGroup != null)
        {
            _overlayCanvasGroup.blocksRaycasts = false;
            _overlayFadeTween = _overlayCanvasGroup.DOFade(0f, _animationDuration * 0.5f)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .OnComplete(() => OnHideAnimationCompleted());
        }
        else
        {
            OnHideAnimationCompleted();
        }

        if (_preGameUIManager != null)
        {
            _preGameUIManager.StopAllAnimations();
            _preGameUIManager.HidePowerUpConfirmationImmediate();
        }

        SetBackgroundRaycastTarget(false);
        AnimateToHiddenState();
    }

    private void OnHideAnimationCompleted()
    {
        ResetOverlayState();
        gameObject.SetActive(false);
    }

    public override IEnumerator ShowRoutine()
    {
        if (_isVisible)
            yield break;

        Show();
        yield return WaitForTweensToFinish();
    }

    public override IEnumerator HideRoutine()
    {
        if (!_isVisible)
            yield break;

        Hide();
        yield return WaitForTweensToFinish();
    }

    protected override void OnDisable()
    {
        KillActiveTweens();
        base.OnDisable();
    }

    private void AnimateToShownState()
    {
        if (_panelTransform == null)
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;

            NotifyPanelShown();
            NotifyUIManagerModalShown();
            OnShown();
            return;
        }

        _moveTween = _panelTransform
            .DOAnchorPos(_shownAnchoredPosition, _animationDuration)
            .SetEase(_openEase)
            .SetUpdate(true);

        _scaleTween = _panelTransform
            .DOScale(_shownScale, _animationDuration)
            .SetEase(_openEase)
            .SetUpdate(true);

        if (_canvasGroup != null)
        {
            _fadeTween = _canvasGroup
                .DOFade(1f, _animationDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        NotifyPanelShown();
        NotifyUIManagerModalShown();
        OnShown();
    }

    private void AnimateToHiddenState()
    {
        if (_panelTransform == null)
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = _hiddenAlpha;

            gameObject.SetActive(false);
            return;
        }

        _moveTween = _panelTransform
            .DOAnchorPos(_shownAnchoredPosition + _hiddenOffset, _animationDuration)
            .SetEase(_closeEase)
            .SetUpdate(true);

        _scaleTween = _panelTransform
            .DOScale(_shownScale * _hiddenScale, _animationDuration)
            .SetEase(_closeEase)
            .SetUpdate(true);

        if (_canvasGroup != null)
        {
            _fadeTween = _canvasGroup
                .DOFade(_hiddenAlpha, _animationDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true);
        }
    }

private void ApplyHiddenState()
    {
        if (_panelTransform != null)
        {
            _panelTransform.anchoredPosition = _shownAnchoredPosition + _hiddenOffset;
            _panelTransform.localScale = _shownScale * _hiddenScale;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = _hiddenAlpha;
        }
    }

    private void ResetOverlayState()
    {
        if (_overlayCanvasGroup != null)
        {
            _overlayCanvasGroup.alpha = 0f;
            _overlayCanvasGroup.blocksRaycasts = false;
        }
    }

    private void KillActiveTweens()
    {
        _moveTween?.Kill();
        _scaleTween?.Kill();
        _fadeTween?.Kill();
        _overlayFadeTween?.Kill();

        _moveTween = null;
        _scaleTween = null;
        _fadeTween = null;
        _overlayFadeTween = null;
    }

    private IEnumerator WaitForTweensToFinish()
    {
        float elapsed = 0f;
        float timeout = Mathf.Max(0.1f, _animationDuration + 0.25f);

        while (HasActiveTween() && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private bool HasActiveTween()
    {
        return IsTweenActive(_moveTween)
            || IsTweenActive(_scaleTween)
            || IsTweenActive(_fadeTween);
    }

    private static bool IsTweenActive(Tween tween)
    {
        return tween != null && tween.IsActive();
    }
}
