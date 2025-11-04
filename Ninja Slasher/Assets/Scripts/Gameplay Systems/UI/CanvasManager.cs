using System.Collections;
using UnityEngine;
using DG.Tweening;

public class CanvasManager : MonoBehaviour
{
    [Header("CANVAS")]
    [SerializeField] private Canvas _splashCanvas;
    [SerializeField] private Canvas _levelsCanvas;
    [SerializeField] private Canvas _preGameCanvas;
    [SerializeField] private Canvas _gameplayCanvas;
    [SerializeField] private Canvas _creditsCanvas;
    [SerializeField] private Canvas _pauseCanvas;
    [SerializeField] private Canvas _profileCanvas;
    [SerializeField] private Canvas _dailyRewardCanvas;
    [SerializeField] private Canvas _noLivesCanvas;
    [SerializeField] private Canvas _resultsCanvas;
    [SerializeField] private Canvas _userIconsCanvas;
    [SerializeField] private Canvas _userNicknameEditCanvas;

    [Header("PANEL REFERENCES")]
    [SerializeField] private RectTransform _profilePanel;
    [SerializeField] private RectTransform _dailyRewardPanel;
    [SerializeField] private RectTransform _resultsPanel;
    [SerializeField] private RectTransform _preGamePanel;
    [SerializeField] private RectTransform _pausePanel;
    [SerializeField] private RectTransform _noLivesPanel;

    [Header("ANIMATION")]
    [SerializeField] private float _animationDuration = 0.1f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InBack;
    [SerializeField] private float _scaleOvershoot = 1.05f;

    private bool _hasAnimatedButtons = false;

    public void OpenCanvas(Canvas canvas) => canvas.enabled = true;
    public void CloseCanvas(Canvas canvas) => canvas.enabled = false;

    public void ShowHideCanvas(Canvas canvas, bool state)
    {
        AudioManager.Instance.PlaySFX(SFXClip.UI_Select);

        if (state)
            ShowCanvasAnimated(canvas);
        else
            HideCanvasAnimated(canvas);
    }

    private void ShowCanvasAnimated(Canvas canvas)
    {
        canvas.enabled = true;

        RectTransform panelToAnimate = GetPanelForCanvas(canvas);
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(canvas);

        if (panelToAnimate != null)
        {
            ShowPanelAnimated(panelToAnimate);
        }
        else
        {
            ShowCanvasGroupAnimated(canvasGroup);
        }
    }

    private void HideCanvasAnimated(Canvas canvas)
    {
        RectTransform panelToAnimate = GetPanelForCanvas(canvas);
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(canvas);

        if (panelToAnimate != null)
        {
            HidePanelAnimated(panelToAnimate, canvas);
        }
        else
        {
            HideCanvasGroupAnimated(canvasGroup, canvas);
        }
    }

    private void ShowPanelAnimated(RectTransform panel)
    {
        DOTween.Kill(panel);

        panel.localScale = Vector3.zero;

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        Sequence showSequence = DOTween.Sequence();
        showSequence.Append(panel.DOScale(_scaleOvershoot, _animationDuration * 0.7f)
            .SetEase(_openEase));
        showSequence.Append(panel.DOScale(1f, _animationDuration * 0.3f)
            .SetEase(Ease.InOutQuad));

        showSequence.SetUpdate(true);
    }

    private void HidePanelAnimated(RectTransform panel, Canvas canvas)
    {
        DOTween.Kill(panel);

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            DOTween.Kill(canvasGroup);
        }

        panel.DOScale(0f, _animationDuration)
            .SetEase(_closeEase)
            .OnComplete(() =>
            {
                canvas.enabled = false;

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }

                panel.localScale = Vector3.one;
            })

            .SetUpdate(true);
    }

    private void ShowCanvasGroupAnimated(CanvasGroup canvasGroup)
    {
        DOTween.Kill(canvasGroup);

        canvasGroup.alpha = 0f;
        canvasGroup.transform.localScale = Vector3.zero;

        Sequence showSequence = DOTween.Sequence();
        showSequence.Append(canvasGroup.DOFade(1f, _animationDuration * 0.6f));
        showSequence.Join(canvasGroup.transform.DOScale(_scaleOvershoot, _animationDuration * 0.7f)
            .SetEase(_openEase));
        showSequence.Append(canvasGroup.transform.DOScale(1f, _animationDuration * 0.3f)
            .SetEase(Ease.InOutQuad));

        showSequence.SetUpdate(true);
    }

    private void HideCanvasGroupAnimated(CanvasGroup canvasGroup, Canvas canvas)
    {
        DOTween.Kill(canvasGroup);

        Sequence hideSequence = DOTween.Sequence();
        hideSequence.Append(canvasGroup.transform.DOScale(0f, _animationDuration)
            .SetEase(_closeEase));
        hideSequence.Join(canvasGroup.DOFade(0f, _animationDuration * 0.8f));
        hideSequence.OnComplete(() => canvas.enabled = false);

        hideSequence.SetUpdate(true);
    }

    private RectTransform GetPanelForCanvas(Canvas canvas)
    {
        if (canvas == _profileCanvas) return _profilePanel;
        if (canvas == _dailyRewardCanvas) return _dailyRewardPanel;
        if (canvas == _resultsCanvas) return _resultsPanel;
        if (canvas == _preGameCanvas) return _preGamePanel;
        if (canvas == _pauseCanvas) return _pausePanel;
        if (canvas == _noLivesCanvas) return _noLivesPanel;

        return null;
    }

    private CanvasGroup GetOrAddCanvasGroup(Canvas canvas)
    {
        CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        return canvasGroup;
    }

    public void ShowHideResultsCanvas()
    {
        bool isCanvasActive = !_resultsCanvas.enabled;
        var panelAnimation = _resultsPanel.GetComponent<Animator>();
        if (isCanvasActive)
        {
            panelAnimation.SetTrigger("Open");
            GetComponent<ResultsUIManager>()?.PrepareResultsIntro();
            ShowCanvasAnimated(_resultsCanvas);
            SetGameplayCanvasEnabled(false);
            StartCoroutine(DelayedResultsShow());
        }
        else
        {
            panelAnimation.SetTrigger("Close");
            HideCanvasAnimated(_resultsCanvas);
        }
    }

    private IEnumerator DelayedResultsShow()
    {
        yield return new WaitForSeconds(_animationDuration);
        GetComponent<ResultsUIManager>()?.ShowResultsPanel();
    }

    public void ShowHideDailyRewardCanvas()
    {
        bool isCanvasActive = !_dailyRewardCanvas.enabled;
        var panelAnimation = _dailyRewardPanel.GetComponent<Animator>();
        if (isCanvasActive)
        {
            panelAnimation.SetTrigger("Open");
            ShowCanvasAnimated(_dailyRewardCanvas);
            GetComponent<DailyRewardUIManager>()?.ShowDailyReward();
        }
        else
        {
            panelAnimation.SetTrigger("Close");
            HideCanvasAnimated(_dailyRewardCanvas);
        }
    }

    public void ShowHideCreditsCanvas()
    {
        bool isCanvasActive = !_creditsCanvas.gameObject.activeInHierarchy;
        ShowHideCanvas(_profileCanvas, !isCanvasActive);

        if (isCanvasActive)
        {
            StartCoroutine(DelayedCreditsShow());
        }
        else
        {
            _creditsCanvas.gameObject.SetActive(isCanvasActive);
            AudioManager.Instance.PlayMusic(MusicClip.MainMenu, !isCanvasActive);
        }
    }

    private IEnumerator DelayedCreditsShow()
    {
        yield return new WaitForSeconds(_animationDuration + 0.1f);
        _creditsCanvas.gameObject.SetActive(true);
        ShowCanvasAnimated(_creditsCanvas);
        AudioManager.Instance.PlayMusic(MusicClip.Credits, true);
    }

    public void ShowHideProfileCanvas()
    {
        bool isCanvasActive = !_profileCanvas.enabled;
        ShowHideCanvas(_profileCanvas, isCanvasActive);
    }

    public void ShowHidePreGameCanvas()
    {
        bool isCanvasActive = !_preGameCanvas.enabled;
        var panelAnimation = _preGamePanel.GetComponent<Animator>();
        if (isCanvasActive)
        {
            panelAnimation.SetTrigger("Open");
            var buttonManager = GetComponent<ButtonManager>();
            if (buttonManager != null)
            {
                buttonManager.StopAllButtonAnimations();
            }

            ShowHideCanvas(_preGameCanvas, isCanvasActive);
            GetComponent<PreGameUIManager>()?.ShowPreGamePowerUps();
        }
        else
        {
            panelAnimation.SetTrigger("Close");
            GetComponent<PreGameUIManager>()?.StopAllAnimations();
            ShowHideCanvas(_preGameCanvas, isCanvasActive);
        }
    }

    public void ShowHidePauseCanvas()
    {
        bool isCanvasActive = !_pauseCanvas.enabled;
        var panelAnimation = _pausePanel.GetComponent<Animator>();
        if (isCanvasActive)
        {
            panelAnimation.SetTrigger("Open");
            ShowHideCanvas(_pauseCanvas, isCanvasActive);
        }
        else
        {
            panelAnimation.SetTrigger("Close");
            ShowHideCanvas(_pauseCanvas, isCanvasActive);
        }
        Time.timeScale = isCanvasActive ? 0 : 1;
    }

    public void SetLevelsCanvasEnabled(bool enabled)
    {
        _levelsCanvas.enabled = enabled;

        if (enabled && !_hasAnimatedButtons)
        {
            _hasAnimatedButtons = true;
            ButtonManager buttonManager = GetComponent<ButtonManager>();
            if (buttonManager != null)
            {
                HideLevelButtons(buttonManager);
            }
            StartCoroutine(TriggerButtonAnimation());
        }
    }

    private IEnumerator TriggerButtonAnimation()
    {
        yield return null;
        yield return new WaitForSeconds(0.3f);

        if (_dailyRewardCanvas.enabled)
        {
            while (_dailyRewardCanvas.enabled)
            {
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(0.5f);
        }

        ButtonManager buttonManager = GetComponent<ButtonManager>();
        if (buttonManager != null)
        {
            buttonManager.TriggerNinjaWaveAnimation();
        }
    }

    private void HideLevelButtons(ButtonManager buttonManager)
    {
        var levelButtons = buttonManager.GetLevelButtons();
        foreach (var button in levelButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }
    }

    public void ShowHideNoLivesCanvas()
    {
        bool isCanvasActive = !_noLivesCanvas.enabled;
        var panelAnimation = _noLivesPanel.GetComponent<Animator>();
        if (isCanvasActive)
        {
            panelAnimation.SetTrigger("Open");
            ShowHideCanvas(_noLivesCanvas, isCanvasActive);
        }
        else
        {
            panelAnimation.SetTrigger("Close");
            ShowHideCanvas(_noLivesCanvas, isCanvasActive);
        }
    }

    private void ToggleCanvas(Canvas canvas)
    {
        bool isCanvasActive = !canvas.enabled;
        ShowHideCanvas(canvas, isCanvasActive);
    }

    public void ShowHideUserNicknameEditCanvas() => ToggleCanvas(_userNicknameEditCanvas);
    public void ShowHideUserIconsCanvas() => ToggleCanvas(_userIconsCanvas);
    public void SetSplashCanvasEnabled(bool enabled) => _splashCanvas.enabled = enabled;
    public void SetGameplayCanvasEnabled(bool enabled) => _gameplayCanvas.enabled = enabled;
    public void SetPauseCanvasEnabled(bool enabled) => _pauseCanvas.enabled = enabled;
    public Canvas GetResultsCanvas() => _resultsCanvas;

    private void OnDisable()
    {
        DOTween.KillAll();
    }
}