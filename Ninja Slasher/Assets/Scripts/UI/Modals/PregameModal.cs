using DG.Tweening;
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

        ApplyHiddenState();
        AnimateToShownState();
    }

    public override void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;

        SetPanelInputEnabled(false);
        KillActiveTweens();
        SetBackgroundRaycastTarget(false);

        if (_preGameUIManager != null)
        {
            _preGameUIManager.StopAllAnimations();
            _preGameUIManager.HidePowerUpConfirmationImmediate();
        }

        NotifyUIManagerModalHidden();
        OnHidden();
        AnimateToHiddenState();
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
            .SetUpdate(true)
            .OnComplete(() => gameObject.SetActive(false));

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

    private void KillActiveTweens()
    {
        _moveTween?.Kill();
        _scaleTween?.Kill();
        _fadeTween?.Kill();

        _moveTween = null;
        _scaleTween = null;
        _fadeTween = null;
    }
}
