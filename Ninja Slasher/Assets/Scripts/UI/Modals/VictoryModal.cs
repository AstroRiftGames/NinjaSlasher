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

    private void OnEnable()
    {
        GameEvents.OnLevelCompleted += OnLevelCompleted;
        GameEvents.OnLevelFailed += OnLevelFailed;
    }

    private void OnDisable()
    {
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
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
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

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

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
        bool isVictory = GameManager.Instance != null && GameManager.Instance.IsVictory;

        if (isVictory)
            AudioService.Instance.PlaySFX(_audioContext.Audio.victory);
        else
            AudioService.Instance.PlaySFX(_audioContext.Audio.defeat);
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