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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnGameDataChanged += OnGameDataChanged;

            var data = SaveManager.Instance.GetGameData();
            if (data != null) OnGameDataChanged(data);
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (SaveManager.Instance != null)
            SaveManager.Instance.OnGameDataChanged -= OnGameDataChanged;
    }

    private void InitializeManagers()
    {
        _canvasManager = GetComponent<CanvasManager>();
        _buttonManager = GetComponent<ButtonManager>();
        _gameplayUIManager = GetComponent<GameplayUIManager>();
        _preGameUIManager = GetComponent<PreGameUIManager>();
        _sceneTransitionManager = GetComponent<SceneTransitionManager>();
    }

    private void Update()
    {
        _gameplayUIManager.UpdateUI();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_gameplayUIManager != null)
            _gameplayUIManager.OnSceneLoaded();
    }


    private void OnGameDataChanged(GameData gd)
    {
        _gameplayUIManager?.UpdateUI();
        _preGameUIManager?.ShowPreGamePowerUps();
        _preGameUIManager?.SetGoals();
    }

    public void ShowLevelSelector() => _sceneTransitionManager.ShowLevelSelector();
    public void LoadLevelScene(string sceneName) => _sceneTransitionManager.LoadLevelScene(sceneName);
    public void RestartLevel() => _sceneTransitionManager.RestartLevel();
    public void ShowConfirmationPanel(string sceneName) => _preGameUIManager.ShowConfirmationPanel(sceneName);
    public void ShowHidePreGameCanvas() => _canvasManager.ShowHidePreGameCanvas();
    public void ShowHidePauseCanvas() => _canvasManager.ShowHidePauseCanvas();
    public void ShowHideCreditsCanvas() => _canvasManager.ShowHideCreditsCanvas();
    public void ShowHideProfileCanvas() => _canvasManager.ShowHideProfileCanvas();
    //public void ShowHideExtraLifeCanvas() => _canvasManager.ShowHideExtraLifeCanvas();
    public void ShowHideDailyRewardCanvas() => _canvasManager.ShowHideDailyRewardCanvas();
    public void ShowHideNoLivesCanvas() => _canvasManager.ShowHideNoLivesCanvas();
    public void ShowLifeLostPanel() => _gameplayUIManager.ShowLifeLostPanel();
    public void HideLifeLostPanel() => _gameplayUIManager.HideLifeLostPanel();
    public void UpdateLivesUI(int lives) => _gameplayUIManager.UpdateLivesUI(lives);
    public void ShowNoLivesPanel() => _gameplayUIManager.ShowNoLivesPanel();
}