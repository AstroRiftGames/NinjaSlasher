using UnityEngine;
using System.Collections;

public class VictoryModal : UIModalBase
{
    [Header("Animation")]
    [SerializeField] private Animator _panelAnimator;
    [SerializeField] private float _closeAnimationDuration = 0.4f;
    [SerializeField] private float _delayBeforeShowingResults = 0.1f;

    private bool _isVictory = false;

    protected override void Awake()
    {
        base.Awake();

        if (_panelAnimator == null)
        {
            _panelAnimator = GetComponentInChildren<Animator>();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        GameEvents.OnLevelCompleted += OnLevelCompleted;
        GameEvents.OnLevelFailed += OnLevelFailed;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEvents.OnLevelCompleted -= OnLevelCompleted;
        GameEvents.OnLevelFailed -= OnLevelFailed;
    }

    private void OnLevelCompleted(LevelStats stats)
    {
        _isVictory = true;
    }

    private void OnLevelFailed(LevelFailedContext ctx)
    {
        _isVictory = false;
    }

    public override void Show()
    {
        if (_isVisible)
        {
            return;
        }

        gameObject.SetActive(true);
        _isVisible = true;

        PlayVictoryAudio();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }

        if (_hasBackground && _backgroundImage != null)
        {
            _backgroundImage.raycastTarget = true;
        }

        if (_panelAnimator != null)
        {
            _panelAnimator.SetTrigger("Open");
        }

        if (ResultsUIManager.Instance != null)
        {
            ResultsUIManager.Instance.PrepareResultsIntro();
        }

        NotifyPanelShown();
        StartCoroutine(ShowResultsDelayed());

        OnShown();
    }

    private IEnumerator ShowResultsDelayed()
    {
        yield return new WaitForSecondsRealtime(_delayBeforeShowingResults);

        if (ResultsUIManager.Instance != null)
        {
            ResultsUIManager.Instance.ShowResultsPanel();
        }
    }

    public override void Hide()
    {
        if (!_isVisible)
        {
            return;
        }

        _isVisible = false;

        SetPanelInputEnabled(false);

        if (_hasBackground && _backgroundImage != null)
        {
            _backgroundImage.raycastTarget = false;
        }

        if (_panelAnimator != null)
        {
            _panelAnimator.SetTrigger("Close");
        }

        OnHidden();

        StartCoroutine(DelayedHide());
    }

    private IEnumerator DelayedHide()
    {
        yield return new WaitForSecondsRealtime(_closeAnimationDuration);
        gameObject.SetActive(false);
    }

    private void PlayVictoryAudio()
    {
        if (AudioService.Instance == null || _audioContext == null || _audioContext.Audio == null)
            return;

        bool isVictory = GameManager.Instance != null && GameManager.Instance.IsVictory;
        AudioEvent clip = isVictory ? _audioContext.Audio.victory : _audioContext.Audio.defeat;

        if (clip != null)
            AudioService.Instance.PlaySFX(clip);
    }

    public void ShowVictory()
    {
        _isVictory = true;
        Show();
    }

    public void ShowDefeat()
    {
        _isVictory = false;
        Show();
    }
}
