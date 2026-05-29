using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CreditsModal : UIModalBase
{
    [SerializeField] private Button _closeButton;
    [SerializeField] private CanvasGroup _argCanvasGroup;
    [SerializeField] private CreditsAutoScroller _creditsAutoScroller;
    [SerializeField] private CanvasGroup _crewCanvasGroup;
    [SerializeField] private CanvasGroup _thanksCanvasGroup;
    [SerializeField] private float _argDisplayDuration = 2f;

    private Tween _argSequenceTween;
    private Tween _thanksFadeTween;
    private Tween _crewFadeTween;

    protected override void Awake()
    {
        base.Awake();
        SetupButtons();
        if (_argCanvasGroup != null)
            _argCanvasGroup.alpha = 0f;
        if (_crewCanvasGroup != null)
            _crewCanvasGroup.alpha = 1f;
    }

    protected override void OnShown()
    {
        MusicEvents.OnEnterCredits?.Invoke();
    }

    protected override void OnShowAnimationCompleted()
    {
        base.OnShowAnimationCompleted();
        PlayARGSequence();
    }
    
    protected override void OnHidden()
    {
        KillARGSequence();
        KillThanksFade();
        KillCrewFade();
        MusicEvents.OnEnterLevelSelection?.Invoke();
    }

    private void PlayARGSequence()
    {
        KillARGSequence();

        if (_argCanvasGroup == null)
        {
            _creditsAutoScroller?.BeginScroll(OnCrewScrollComplete);
            return;
        }

        Sequence seq = DOTween.Sequence();
        seq.Append(_argCanvasGroup.DOFade(1f, 0.5f).SetUpdate(true));
        seq.AppendInterval(_argDisplayDuration);
        seq.Append(_argCanvasGroup.DOFade(0f, 0.5f).SetUpdate(true));
        seq.OnComplete(() => _creditsAutoScroller?.BeginScroll(OnCrewScrollComplete));
        _argSequenceTween = seq;
    }

    private void OnCrewScrollComplete()
    {
        KillCrewFade();
        KillThanksFade();

        Sequence seq = DOTween.Sequence();
        if (_crewCanvasGroup != null)
            seq.Append(_crewCanvasGroup.DOFade(0f, 0.5f).SetUpdate(true));
        if (_thanksCanvasGroup != null)
            seq.Append(_thanksCanvasGroup.DOFade(1f, 1f).SetUpdate(true));
        _crewFadeTween = seq;
    }

    private void KillCrewFade()
    {
        if (_crewFadeTween == null) return;
        _crewFadeTween.Kill();
        _crewFadeTween = null;
    }

    private void KillThanksFade()
    {
        if (_thanksFadeTween == null) return;
        _thanksFadeTween.Kill();
        _thanksFadeTween = null;
    }

    private void KillARGSequence()
    {
        if (_argSequenceTween == null) return;
        _argSequenceTween.Kill();
        _argSequenceTween = null;
    }

    private void SetupButtons()
    {
        if (_closeButton == null)
        {
            Debug.LogWarning("[CreditsModal] Close button is not assigned.");
            return;
        }

        _closeButton.onClick.AddListener(OnCloseClicked);
    }

    private void OnCloseClicked()
    {
        UIEvents.RequestHideCreditsModal();
    }

    private void OnDestroy()
    {
        _closeButton?.onClick.RemoveListener(OnCloseClicked);
    }
}
