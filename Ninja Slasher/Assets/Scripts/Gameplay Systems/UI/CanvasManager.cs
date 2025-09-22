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

    [Header("ANIMATION SETTINGS")]
    [SerializeField] private float _animationDuration = 0.3f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InBack;
    [SerializeField] private Vector3 _popScaleMultiplier = new Vector3(1.1f, 1.1f, 1f);

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
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(canvas);

        canvasGroup.alpha = 0f;
        canvasRect.localScale = Vector3.zero;

        Sequence openSequence = DOTween.Sequence();

        openSequence.Append(canvasGroup.DOFade(1f, _animationDuration * 0.6f));
        openSequence.Join(canvasRect.DOScale(Vector3.one, _animationDuration)
            .SetEase(_openEase));

        openSequence.Append(canvasRect.DOScale(_popScaleMultiplier, 0.1f)
            .SetEase(Ease.OutQuad));
        openSequence.Append(canvasRect.DOScale(Vector3.one, 0.1f)
            .SetEase(Ease.InQuad));
    }

    private void HideCanvasAnimated(Canvas canvas)
    {
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(canvas);

        Sequence closeSequence = DOTween.Sequence();

        closeSequence.Append(canvasRect.DOScale(Vector3.zero, _animationDuration)
            .SetEase(_closeEase));
        closeSequence.Join(canvasGroup.DOFade(0f, _animationDuration * 0.8f));

        closeSequence.OnComplete(() => canvas.enabled = false);
    }

    private CanvasGroup GetOrAddCanvasGroup(Canvas canvas)
    {
        CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        return canvasGroup;
    }

    public void ShowConfigDropdownAnimated(RectTransform configPanel, bool isOpen)
    {
        if (isOpen)
        {
            Vector2 startPos = configPanel.anchoredPosition + new Vector2(0, 200f);
            configPanel.anchoredPosition = startPos;
            configPanel.gameObject.SetActive(true);

            configPanel.DOAnchorPosY(startPos.y - 200f, _animationDuration)
                .SetEase(Ease.OutBounce);
        }
        else
        {
            configPanel.DOAnchorPosY(configPanel.anchoredPosition.y + 200f, _animationDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() => configPanel.gameObject.SetActive(false));
        }
    }

    public void ShowHideResultsCanvas()
    {
        bool isCanvasActive = !_resultsCanvas.enabled;

        if (isCanvasActive)
        {
            ShowResultsCanvasWithSequence();
        }
        else
        {
            HideCanvasAnimated(_resultsCanvas);
        }
    }

    private void ShowResultsCanvasWithSequence()
    {
        _resultsCanvas.enabled = true;
        RectTransform canvasRect = _resultsCanvas.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(_resultsCanvas);

        canvasGroup.alpha = 0f;
        canvasRect.localScale = Vector3.zero;

        AudioManager.Instance.PlaySFX(SFXClip.UI_Select);

        Sequence panelSequence = DOTween.Sequence();

        panelSequence.Append(canvasGroup.DOFade(1f, _animationDuration * 0.6f));
        panelSequence.Join(canvasRect.DOScale(Vector3.one, _animationDuration)
            .SetEase(_openEase));

        panelSequence.Append(canvasRect.DOScale(_popScaleMultiplier, 0.1f)
            .SetEase(Ease.OutQuad));
        panelSequence.Append(canvasRect.DOScale(Vector3.one, 0.1f)
            .SetEase(Ease.InQuad));

        panelSequence.OnComplete(() => {
            StartCoroutine(DelayedResultsShow());
        });
    }

    private IEnumerator DelayedResultsShow()
    {
        yield return new WaitForSeconds(0.1f);
        GetComponent<ResultsUIManager>().ShowResultsPanel();
    }

    public void ShowHideUserNicknameEditCanvas()
    {
        bool isCanvasActive = !_userNicknameEditCanvas.enabled;
        ShowHideCanvas(_userNicknameEditCanvas, isCanvasActive);
    }

    public void ShowHideUserIconsCanvas()
    {
        bool isCanvasActive = !_userIconsCanvas.enabled;
        ShowHideCanvas(_userIconsCanvas, isCanvasActive);
    }

    public void ShowHideDailyRewardCanvas()
    {
        bool isCanvasActive = !_dailyRewardCanvas.enabled;

        if (isCanvasActive)
        {
            ShowCanvasAnimated(_dailyRewardCanvas);
            GetComponent<DailyRewardUIManager>().ShowDailyReward();
        }
        else
        {
            HideCanvasAnimated(_dailyRewardCanvas);
        }
    }

    public void ShowHideNoLivesCanvas()
    {
        bool isCanvasActive = !_noLivesCanvas.enabled;
        ShowHideCanvas(_noLivesCanvas, isCanvasActive);
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
        RectTransform creditsRect = _creditsCanvas.GetComponent<RectTransform>();
        CanvasGroup creditsGroup = GetOrAddCanvasGroup(_creditsCanvas);

        creditsGroup.alpha = 0f;
        creditsRect.anchoredPosition = new Vector2(creditsRect.anchoredPosition.x, -500f);

        Sequence creditsSequence = DOTween.Sequence();
        creditsSequence.Append(creditsGroup.DOFade(1f, _animationDuration));
        creditsSequence.Join(creditsRect.DOAnchorPosY(0f, _animationDuration)
            .SetEase(Ease.OutBack));

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

        if (isCanvasActive)
        {
            ShowCanvasAnimated(_preGameCanvas);
            GetComponent<PreGameUIManager>().ShowPreGamePowerUps();
        }
        else
        {
            HideCanvasAnimated(_preGameCanvas);
        }
    }

    public void ShowHidePauseCanvas()
    {
        bool isCanvasActive = !_pauseCanvas.enabled;
        ShowHideCanvas(_pauseCanvas, isCanvasActive);
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

    public void SetSplashCanvasEnabled(bool enabled) => _splashCanvas.enabled = enabled;
    public void SetGameplayCanvasEnabled(bool enabled) => _gameplayCanvas.enabled = enabled;
    public void SetPauseCanvasEnabled(bool enabled) => _pauseCanvas.enabled = enabled;

    public void SetAnimationParameters(float duration, Ease openEase, Ease closeEase)
    {
        _animationDuration = duration;
        _openEase = openEase;
        _closeEase = closeEase;
    }

    private void OnDisable()
    {
        DOTween.KillAll();
    }
}