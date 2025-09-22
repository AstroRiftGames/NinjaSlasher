using System.Collections;
using UnityEngine;

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

    private bool _hasAnimatedButtons = false;

    public void OpenCanvas(Canvas canvas) => canvas.enabled = true;
    public void CloseCanvas(Canvas canvas) => canvas.enabled = false;

    public void ShowHideCanvas(Canvas canvas, bool state)
    {
        AudioManager.Instance.PlaySFX(SFXClip.UI_Select);
        if (state) OpenCanvas(canvas);
        else CloseCanvas(canvas);
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

    public void ShowHideResultsCanvas()
    {
        bool isCanvasActive = !_resultsCanvas.enabled;
        ShowHideCanvas(_resultsCanvas, isCanvasActive);
        if (isCanvasActive)
            GetComponent<ResultsUIManager>().ShowResultsPanel();
    }

    public void ShowHideDailyRewardCanvas()
    {
        bool isCanvasActive = !_dailyRewardCanvas.enabled;
        ShowHideCanvas(_dailyRewardCanvas, isCanvasActive);
        if (isCanvasActive)
            GetComponent<DailyRewardUIManager>().ShowDailyReward();
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
        _creditsCanvas.gameObject.SetActive(isCanvasActive);

        if (isCanvasActive)
            AudioManager.Instance.PlayMusic(MusicClip.Credits, isCanvasActive);
        else 
            AudioManager.Instance.PlayMusic(MusicClip.MainMenu, !isCanvasActive);
    }
    
    public void ShowHideProfileCanvas()
    {
        bool isCanvasActive = !_profileCanvas.enabled;
        ShowHideCanvas(_profileCanvas, isCanvasActive);
    }

    public void ShowHidePreGameCanvas()
    {
        bool isCanvasActive = !_preGameCanvas.enabled;
        ShowHideCanvas(_preGameCanvas, isCanvasActive);
        if (isCanvasActive)
            GetComponent<PreGameUIManager>().ShowPreGamePowerUps();
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
}