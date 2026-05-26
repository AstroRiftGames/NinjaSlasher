using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public abstract class UIOverlayBase : UIPanel
{
    private const string OpenTriggerName = "Open";
    private const string CloseTriggerName = "Close";
    private const string OpenStateName = "Open";
    private const string CloseStateName = "Close";

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

    private Sequence _panelSequence;

    protected override bool BlocksUnderlyingUI => true;

    protected override void Awake()
    {
        base.Awake();

        if (_panelTransform == null)
            _panelTransform = GetComponent<RectTransform>();

        _panelTransform = ResolvePreferredPanelTransform(_backgroundImage != null ? _backgroundImage.transform : null);

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        ApplyHiddenVisualState();
        SetPanelInputEnabled(false);
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

    public override void HideImmediate()
    {
        if (!_isVisible && !gameObject.activeSelf)
            return;

        KillActiveSequence();
        _isVisible = false;
        SetPanelInputEnabled(false);
        OnHidden();
        ApplyHiddenVisualState();
        gameObject.SetActive(false);
        OnHideAnimationCompleted();
    }

    public override IEnumerator ShowRoutine()
    {
        if (_isVisible)
            yield break;

        Show();
        yield return WaitForSequenceToFinish();
    }

    public override IEnumerator HideRoutine()
    {
        if (!_isVisible)
            yield break;

        Hide();
        yield return WaitForSequenceToFinish();
    }

    protected virtual void AnimateShow()
    {
        DOTween.Kill(_backgroundImage);
        DOTween.Kill(_canvasGroup);
        DOTween.Kill(_panelTransform);
        KillActiveSequence();

        float maxFadeDuration = Mathf.Max(_backgroundFadeDuration, _contentFadeDuration);
        if (_useContentScaleAnimation)
            maxFadeDuration = Mathf.Max(maxFadeDuration, _contentShowScaleDuration);

        _panelSequence = DOTween.Sequence();

        if (_backgroundImage != null)
        {
            _panelSequence.Insert(0f, _backgroundImage.DOFade(_backgroundColor.a, _backgroundFadeDuration)
                .SetEase(_fadeEase));
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _panelSequence.Insert(0f, _canvasGroup.DOFade(1f, _contentFadeDuration)
                .SetEase(_fadeEase));
        }

        if (_panelTransform != null && _useContentScaleAnimation)
        {
            _panelTransform.localScale = Vector3.one * _hiddenContentScaleMultiplier;
            _panelSequence.Insert(0f, _panelTransform.DOScale(1f, _contentShowScaleDuration)
                .SetEase(_contentShowScaleEase));
        }

        if (_panelAnimator != null)
        {
            PrepareAnimatorForOpenPlayback();

            float remainingAnimatorDuration = Mathf.Max(0f, _animatorOpenDuration - maxFadeDuration);
            if (remainingAnimatorDuration > 0f)
                _panelSequence.AppendInterval(remainingAnimatorDuration);
        }

        _panelSequence
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _panelSequence = null;
                OnShowAnimationCompleted();
            });
    }

    protected virtual void AnimateHide()
    {
        DOTween.Kill(_backgroundImage);
        DOTween.Kill(_canvasGroup);
        DOTween.Kill(_panelTransform);
        KillActiveSequence();

        // Deshabilitar raycasts al inicio del cierre, no al final de la animación
        if (!_blockRaycastsWhenHidden)
            SetPanelInputEnabled(false);

        _panelSequence = DOTween.Sequence();

        float currentTime = 0f;

        if (_panelAnimator != null)
        {
            _panelSequence.InsertCallback(currentTime, () =>
            {
                _panelAnimator.ResetTrigger(OpenTriggerName);
                _panelAnimator.ResetTrigger(CloseTriggerName);
                _panelAnimator.Play(CloseStateName, 0, 0f);
                _panelAnimator.Update(0f);
            });

            currentTime += _animatorCloseDuration;
        }

        if (_canvasGroup != null)
        {
            _panelSequence.Insert(currentTime, _canvasGroup.DOFade(0f, _contentFadeDuration)
                .SetEase(Ease.InQuad));
        }

        if (_panelTransform != null && _useContentScaleAnimation)
        {
            _panelSequence.Insert(currentTime, _panelTransform.DOScale(_hiddenContentScaleMultiplier, _contentHideScaleDuration)
                .SetEase(_contentHideScaleEase));
        }

        if (_backgroundImage != null)
        {
            _panelSequence.Insert(currentTime, _backgroundImage.DOFade(0f, _backgroundFadeDuration)
                .SetEase(Ease.InQuad));
        }

        _panelSequence.OnComplete(() =>
        {
            _panelSequence = null;
            gameObject.SetActive(false);
            ApplyHiddenVisualState();
            OnHideAnimationCompleted();
        });

        _panelSequence.SetUpdate(true);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        DOTween.Kill(_backgroundImage);
        DOTween.Kill(_canvasGroup);
        DOTween.Kill(_panelTransform);
        KillActiveSequence();
        ApplyHiddenVisualState();

        if (!_blockRaycastsWhenHidden)
            SetPanelInputEnabled(false);
    }

    private IEnumerator WaitForSequenceToFinish()
    {
        while (_panelSequence != null && _panelSequence.IsActive())
        {
            yield return null;
        }
    }

    private void KillActiveSequence()
    {
        if (_panelSequence == null)
            return;

        _panelSequence.Kill();
        _panelSequence = null;
    }

    private void ApplyHiddenVisualState()
    {
        PrepareAnimatorForHiddenState();

        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;

        if (_panelTransform != null)
            _panelTransform.localScale = _useContentScaleAnimation
                ? Vector3.one * _hiddenContentScaleMultiplier
                : Vector3.one;

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

    private void PrepareAnimatorForOpenPlayback()
    {
        if (_panelAnimator == null)
            return;

        _panelAnimator.Rebind();
        _panelAnimator.ResetTrigger(OpenTriggerName);
        _panelAnimator.ResetTrigger(CloseTriggerName);
        _panelAnimator.Play(OpenStateName, 0, 0f);
        _panelAnimator.Update(0f);
    }

    private void PrepareAnimatorForHiddenState()
    {
        if (_panelAnimator == null)
            return;

        _panelAnimator.Rebind();
        _panelAnimator.ResetTrigger(OpenTriggerName);
        _panelAnimator.ResetTrigger(CloseTriggerName);
        _panelAnimator.Play(CloseStateName, 0, 1f);
        _panelAnimator.Update(0f);
    }
}
