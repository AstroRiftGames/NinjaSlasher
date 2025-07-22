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
    [SerializeField] private Canvas _configCanvas;

    public void OpenCanvas(Canvas canvas) => canvas.enabled = true;
    public void CloseCanvas(Canvas canvas) => canvas.enabled = false;

    public void ShowHideCanvas(Canvas canvas, bool state)
    {
        if (state) OpenCanvas(canvas);
        else CloseCanvas(canvas);
    }

    public void ShowHideCreditsCanvas()
    {
        bool isCanvasActive = !_creditsCanvas.enabled;
        ShowHideCanvas(_levelsCanvas, !isCanvasActive);
        ShowHideCanvas(_creditsCanvas, isCanvasActive);
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

    public void SetLevelsCanvasEnabled(bool enabled) => _levelsCanvas.enabled = enabled;
    public void SetSplashCanvasEnabled(bool enabled) => _splashCanvas.enabled = enabled;
    public void SetGameplayCanvasEnabled(bool enabled) => _gameplayCanvas.enabled = enabled;
    public void SetPauseCanvasEnabled(bool enabled) => _pauseCanvas.enabled = enabled;
}