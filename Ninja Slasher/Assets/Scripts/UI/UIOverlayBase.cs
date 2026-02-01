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

    [Header("Animator Settings")]
    [SerializeField] protected Animator _panelAnimator;
    [SerializeField] protected float _animatorOpenDuration = 0.35f;
    [SerializeField] protected float _animatorCloseDuration = 0.35f;

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

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = true;
        }

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

        float maxFadeDuration = Mathf.Max(_backgroundFadeDuration, _contentFadeDuration);

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

        if (_panelAnimator != null)
        {
            showSequence.InsertCallback(maxFadeDuration, () =>
            {
                _panelAnimator.SetTrigger("Open");
            });
        }

        showSequence.SetUpdate(true);

        //AudioManager.Instance?.PlaySFX(SFXClip.UI_Select);
        AudioService.Instance?.PlaySFX(_audioContext.Audio.select);
    }

    protected virtual void AnimateHide()
    {
        DOTween.Kill(_backgroundImage);
        DOTween.Kill(_canvasGroup);

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

        if (_backgroundImage != null)
        {
            hideSequence.Insert(currentTime, _backgroundImage.DOFade(0f, _backgroundFadeDuration)
                .SetEase(Ease.InQuad));
        }

        hideSequence.OnComplete(() =>
        {
            if (_canvasGroup != null && !_blockRaycastsWhenHidden)
                _canvasGroup.blocksRaycasts = false;

            gameObject.SetActive(false);

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;

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

    protected virtual void OnDisable()
    {
        DOTween.Kill(_backgroundImage);
        DOTween.Kill(_canvasGroup);
    }
}