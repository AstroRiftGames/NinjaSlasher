using System.Collections;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

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

    [Header("SLASH ANIMATION")]
    [SerializeField] private float _animationDuration = 0.5f;
    [SerializeField] private float _slashDistance = 300f;
    [SerializeField] private Ease _slashEase = Ease.OutBack;

    private Dictionary<RectTransform, Vector2> _originalPositions = new Dictionary<RectTransform, Vector2>();
    private Dictionary<RectTransform, Vector3> _originalScales = new Dictionary<RectTransform, Vector3>();

    private bool _hasAnimatedButtons = false;

    public void OpenCanvas(Canvas canvas) => canvas.enabled = true;
    public void CloseCanvas(Canvas canvas) => canvas.enabled = false;

    public void ShowHideCanvas(Canvas canvas, bool state)
    {
        AudioManager.Instance.PlaySFX(SFXClip.UI_Select);

        canvas.enabled = state;

        RectTransform panelToAnimate = GetPanelForCanvas(canvas);

        if (panelToAnimate != null)
        {
            if (state)
                ShowPanelSlashAnimation(panelToAnimate);
            else
                HidePanelSlashAnimation(panelToAnimate, canvas);
        }
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

    private void ShowPanelSlashAnimation(RectTransform panel)
    {
        if (panel == null) return;

        DOTween.Kill(panel);

        Vector2 originalPos = GetOriginalPosition(panel);
        Vector3 originalScale = GetOriginalScale(panel);

        panel.localScale = new Vector3(0.05f, 0.05f, 1f);
        panel.anchoredPosition = originalPos + new Vector2(_slashDistance, _slashDistance);

        Sequence slashSequence = DOTween.Sequence();

        slashSequence.Append(panel.DOAnchorPos(originalPos, _animationDuration * 0.7f)
            .SetEase(Ease.InOutQuad));

        slashSequence.Append(panel.DOScale(originalScale, _animationDuration * 0.3f)
            .SetEase(_slashEase));
    }

    private void HidePanelSlashAnimation(RectTransform panel, Canvas canvas)
    {
        if (panel == null)
        {
            canvas.enabled = false;
            return;
        }

        DOTween.Kill(panel);

        Vector2 currentPos = panel.anchoredPosition;

        Sequence hideSequence = DOTween.Sequence();

        hideSequence.Append(panel.DOAnchorPos(currentPos + new Vector2(_slashDistance, _slashDistance), _animationDuration)
            .SetEase(Ease.InBack));
        hideSequence.Join(panel.DOScale(Vector3.zero, _animationDuration)
            .SetEase(Ease.InBack));

        hideSequence.OnComplete(() => {
            panel.anchoredPosition = GetOriginalPosition(panel);
            panel.localScale = GetOriginalScale(panel);
            canvas.enabled = false;
        });
    }

    private Vector2 GetOriginalPosition(RectTransform panel)
    {
        if (_originalPositions.ContainsKey(panel))
        {
            return _originalPositions[panel];
        }

        Vector2 pos = panel.anchoredPosition;
        _originalPositions[panel] = pos;
        return pos;
    }

    private Vector3 GetOriginalScale(RectTransform panel)
    {
        if (_originalScales.ContainsKey(panel))
        {
            return _originalScales[panel];
        }

        Vector3 scale = panel.localScale;
        _originalScales[panel] = scale;
        return scale;
    }

    public void ShowHideResultsCanvas()
    {
        bool isCanvasActive = !_resultsCanvas.enabled;

        if (isCanvasActive)
        {
            _resultsCanvas.enabled = true;
            if (_resultsPanel != null)
            {
                ShowPanelSlashAnimation(_resultsPanel);
                StartCoroutine(DelayedResultsShow());
            }
            else
            {
                GetComponent<ResultsUIManager>()?.ShowResultsPanel();
            }
        }
        else
        {
            HidePanelSlashAnimation(_resultsPanel, _resultsCanvas);
        }
    }

    private IEnumerator DelayedResultsShow()
    {
        yield return new WaitForSeconds(_animationDuration + 0.1f);
        GetComponent<ResultsUIManager>()?.ShowResultsPanel();
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
            _dailyRewardCanvas.enabled = true;
            if (_dailyRewardPanel != null)
            {
                ShowPanelSlashAnimation(_dailyRewardPanel);
            }
            GetComponent<DailyRewardUIManager>()?.ShowDailyReward();
        }
        else
        {
            HidePanelSlashAnimation(_dailyRewardPanel, _dailyRewardCanvas);
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
            _preGameCanvas.enabled = true;
            if (_preGamePanel != null)
            {
                ShowPanelSlashAnimation(_preGamePanel);
            }
            GetComponent<PreGameUIManager>()?.ShowPreGamePowerUps();
        }
        else
        {
            HidePanelSlashAnimation(_preGamePanel, _preGameCanvas);
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

    private void OnDisable()
    {
        DOTween.KillAll();
    }
}