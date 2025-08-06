using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviourSingleton<UIManager>
{
    [Header("MANAGERS")]
    private CanvasManager _canvasManager;
    private ButtonManager _buttonManager;
    private GameplayUIManager _gameplayUIManager;
    private PreGameUIManager _preGameUIManager;
    private SceneTransitionManager _sceneTransitionManager;
    private LifeManagerUI _lifeManagerUI;

    public override void Awake()
    {
        base.Awake();
        InitializeManagers();
    }

    private void Start()
    {
        _buttonManager.SetupButtons();
        _gameplayUIManager.Initialize();
    }

    private void InitializeManagers()
    {
        _canvasManager = GetComponent<CanvasManager>();
        _buttonManager = GetComponent<ButtonManager>();
        _gameplayUIManager = GetComponent<GameplayUIManager>();
        _preGameUIManager = GetComponent<PreGameUIManager>();
        _sceneTransitionManager = GetComponent<SceneTransitionManager>();
        _lifeManagerUI = GetComponent<LifeManagerUI>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_gameplayUIManager != null)
            _gameplayUIManager.OnSceneLoaded();
    }

    public void ShowLevelSelector() => _sceneTransitionManager.ShowLevelSelector();
    public void LoadLevelScene(string sceneName) => _sceneTransitionManager.LoadLevelScene(sceneName);
    public void RestartLevel() => _sceneTransitionManager.RestartLevel();
    public void ShowConfirmationPanel(string sceneName) => _preGameUIManager.ShowConfirmationPanel(sceneName);
    public void ShowHidePreGameCanvas() => _canvasManager.ShowHidePreGameCanvas();
    public void ShowHidePauseCanvas() => _canvasManager.ShowHidePauseCanvas();
    public void ShowLifeLostPanel() => _gameplayUIManager.ShowLifeLostPanel();
    public void HideLifeLostPanel() => _gameplayUIManager.HideLifeLostPanel();
    public void UpdateLivesUI(int lives) => _gameplayUIManager.UpdateLivesUI(lives);
    public void ShowNoLivesPanel() => _gameplayUIManager.ShowNoLivesPanel();
    public void SetCounter(int time) => _lifeManagerUI.SetCounter(time);
    public void OnTimerEnd() => _lifeManagerUI.OnCounterEnd();
}