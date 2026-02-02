using UnityEngine;
using DG.Tweening;

public abstract class UIPopupBase : UIPanel
{
    [Header("Popup Animation")]
    [SerializeField] protected float _animationDuration = 0.3f;
    [SerializeField] protected Ease _openEase = Ease.OutBack;
    [SerializeField] protected Ease _closeEase = Ease.InBack;
    [SerializeField] protected float _scaleOvershoot = 1.1f;

    protected override void Awake()
    {
        base.Awake();

        if (_panelTransform == null)
            _panelTransform = GetComponent<RectTransform>();
    }

    public override void Show()
    {
        if (_isVisible) return;

        gameObject.SetActive(true);
        _isVisible = true;

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
        DOTween.Kill(_panelTransform);

        _panelTransform.localScale = Vector3.zero;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        Sequence showSequence = DOTween.Sequence();
        showSequence.Append(_panelTransform.DOScale(_scaleOvershoot, _animationDuration * 0.7f)
            .SetEase(_openEase));
        showSequence.Append(_panelTransform.DOScale(1f, _animationDuration * 0.3f)
            .SetEase(Ease.InOutQuad));

        showSequence.SetUpdate(true);

        //AudioManager.Instance?.PlaySFX(SFXClip.UI_Select);
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
                gameObject.SetActive(false);

                if (_canvasGroup != null)
                    _canvasGroup.alpha = 1f;

                _panelTransform.localScale = Vector3.one;
            })
            .SetUpdate(true);
    }

    protected virtual void OnDisable()
    {
        DOTween.Kill(_panelTransform);
    }
}