using Managers;
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

    public bool IsHapticFeedbackActive => _isHapticFeedbackActive;
    private bool _isHapticFeedbackActive = true;

    private bool _isInitialized = false;

    public override void Awake()
    {
        base.Awake();

        if (this != Instance)
        {
            return;
        }

        InitializeManagers();
    }

    private void OnEnable()
    {
        if (this != Instance) return;

        SceneManager.sceneLoaded += OnSceneLoaded;

        StartCoroutine(SafeSubscribeToCustomUpdate());
    }

    private IEnumerator SafeSubscribeToCustomUpdate()
    {
        while (CustomUpdateManager.Instance == null)
        {
            yield return null;
        }

        CustomUpdateManager.Instance.SubscribeToUpdate(CustomUpdate);
    }

    private void OnDisable()
    {
        if (this != Instance) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (CustomUpdateManager.Instance != null)
        {
            CustomUpdateManager.Instance.UnsubscribeFromUpdate(CustomUpdate);
        }
    }

    private void Start()
    {
        if (this != Instance) return;

        StartCoroutine(InitializeUI());
    }

    private IEnumerator InitializeUI()
    {
        yield return new WaitForEndOfFrame();

        while (SaveManager.Instance == null ||
               !SaveManager.Instance.IsDataLoaded)
        {
            yield return null;
        }

        _buttonManager.SetupButtons();
        _gameplayUIManager.Initialize();

        _isInitialized = true;
    }

    private void InitializeManagers()
    {
        _canvasManager = GetComponent<CanvasManager>();
        _buttonManager = GetComponent<ButtonManager>();
        _gameplayUIManager = GetComponent<GameplayUIManager>();
        _preGameUIManager = GetComponent<PreGameUIManager>();
        _sceneTransitionManager = GetComponent<SceneTransitionManager>();
    }

    private void CustomUpdate()
    {
        if (_isInitialized)
        {
            _gameplayUIManager.UpdateUI();

            if (Input.GetKeyDown(KeyCode.T))
            {
                Cursor.visible = !Cursor.visible;
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_gameplayUIManager != null)
            _gameplayUIManager.OnSceneLoaded();
    }

    public void OpenURL(string url)
    {
        Application.OpenURL(url);
    }

    public void ShowLevelSelector() => _sceneTransitionManager.ShowLevelSelector();
    public void LoadLevelScene(string sceneName) => _sceneTransitionManager.LoadLevelScene(sceneName);
    public void RestartLevel() => _sceneTransitionManager.RestartLevel();
    public void ShowConfirmationPanel(string sceneName) => _preGameUIManager.ShowConfirmationPanel(sceneName);
    public void ShowHidePreGameCanvas() => _canvasManager.ShowHidePreGameCanvas();
    public void ShowHidePauseCanvas() => _canvasManager.ShowHidePauseCanvas();
    public void ShowHideCreditsCanvas() => _canvasManager.ShowHideCreditsCanvas();
    public void ShowHideProfileCanvas() => _canvasManager.ShowHideProfileCanvas();
    public void SwitchHapticFeedback() => _isHapticFeedbackActive = !_isHapticFeedbackActive;
    public void ShowHideUserIconsCanvas() => _canvasManager.ShowHideUserIconsCanvas();
    public void ShowHideUserNicknameEditCanvas() => _canvasManager.ShowHideUserNicknameEditCanvas();
    public void ShowHideResultsCanvas() => _canvasManager.ShowHideResultsCanvas();
    public void ShowHideLifeLostCanvas() => _canvasManager.ShowHideLifeLostCanvas();
    public void ShowHideDailyRewardCanvas() => _canvasManager.ShowHideDailyRewardCanvas();
    public void ShowHideNoLivesCanvas() => _canvasManager.ShowHideNoLivesCanvas();
    //public void ShowLifeLostPanel() => _gameplayUIManager.ShowLifeLostPanel();
    //public void HideLifeLostPanel() => _gameplayUIManager.HideLifeLostPanel();
    public void UpdateLivesUI(int lives) => _gameplayUIManager.UpdateLivesUI(lives);
    public void ShowNoLivesPanel() => _gameplayUIManager.ShowNoLivesPanel();
}