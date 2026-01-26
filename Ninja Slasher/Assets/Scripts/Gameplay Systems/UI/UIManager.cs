using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using AstroRift.Core.Update;

public class UIManager : MonoBehaviourSingleton<UIManager>
{
    [Header("MANAGERS")]
    private CanvasManager _canvasManager;
    private ButtonManager _buttonManager;
    private GameplayUIManager _gameplayUIManager;
    private PreGameUIManager _preGameUIManager;
    private SceneTransitionManager _sceneTransitionManager;

    [Header("OVERLAYS")]
    [SerializeField] private PauseOverlay _pauseOverlay;
    [SerializeField] private NoLivesOverlay _noLivesOverlay;
    [SerializeField] private LifeLostOverlay _lifeLostOverlay;

    [Header("SCREENS")]
    [SerializeField] private SplashScreen _splashScreen;

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

        // TO DO: FUTURO: Migrar a UIEvents
        // SubscribeToUIEvents();
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

        // TO DO: FUTURO: Migrar a UIEvents
        // UnsubscribeFromUIEvents();
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

        Debug.Log("[UIManager] Inicializacion completa");
    }

    private void InitializeManagers()
    {
        _canvasManager = GetComponent<CanvasManager>();
        _buttonManager = GetComponent<ButtonManager>();
        _gameplayUIManager = GetComponent<GameplayUIManager>();
        _preGameUIManager = GetComponent<PreGameUIManager>();
        _sceneTransitionManager = GetComponent<SceneTransitionManager>();

        if (_canvasManager == null)
            Debug.LogError("[UIManager] CanvasManager no encontrado");
        if (_buttonManager == null)
            Debug.LogError("[UIManager] ButtonManager no encontrado");
        if (_gameplayUIManager == null)
            Debug.LogError("[UIManager] GameplayUIManager no encontrado");
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

    // FUTURO: Metodos para migrar a UIEvents
    /*
    private void SubscribeToUIEvents()
    {
        UIEvents.OnPanelOpenRequested += HandlePanelOpenRequest;
        UIEvents.OnPanelCloseRequested += HandlePanelCloseRequest;
        UIEvents.OnSceneTransitionRequested += HandleSceneTransition;
    }

    private void UnsubscribeFromUIEvents()
    {
        UIEvents.OnPanelOpenRequested -= HandlePanelOpenRequest;
        UIEvents.OnPanelCloseRequested -= HandlePanelCloseRequest;
        UIEvents.OnSceneTransitionRequested -= HandleSceneTransition;
    }

    private void HandlePanelOpenRequest(string panelName)
    {
        // Logica para abrir paneles por nombre
    }

    private void HandlePanelCloseRequest(string panelName)
    {
        // Logica para cerrar paneles por nombre
    }

    private void HandleSceneTransition(string sceneName)
    {
        _sceneTransitionManager.LoadLevelScene(sceneName);
    }
    */

    #region OVERLAYS

    public void ShowPauseOverlay()
    {
        if (_pauseOverlay != null)
            _pauseOverlay.Show();
    }

    public void HidePauseOverlay()
    {
        if (_pauseOverlay != null)
            _pauseOverlay.Hide();
    }

    public void TogglePauseOverlay()
    {
        if (_pauseOverlay == null) return;

        if (_pauseOverlay.IsVisible)
            _pauseOverlay.Hide();
        else
            _pauseOverlay.Show();
    }

    public void ShowNoLivesOverlay()
    {
        if (_noLivesOverlay != null)
            _noLivesOverlay.Show();
    }

    public void HideNoLivesOverlay()
    {
        if (_noLivesOverlay != null)
            _noLivesOverlay.Hide();
    }

    public void ShowLifeLostOverlay(int livesRemaining)
    {
        if (_lifeLostOverlay != null)
            _lifeLostOverlay.ShowLifeLost(livesRemaining);
    }

    #endregion

    #region SCREENS (MainCanvas)

    public void ShowSplashScreen()
    {
        if (_splashScreen != null)
            _splashScreen.Show();
    }

    public void HideSplashScreen()
    {
        if (_splashScreen != null)
            _splashScreen.Hide();
    }

    //public void SetSplashCanvasEnabled(bool enabled)
    //{
    //    if (_splashScreen != null)
    //    {
    //        if (enabled)
    //            _splashScreen.Show();
    //        else
    //            _splashScreen.Hide();
    //    }
    //    else if (_canvasManager != null)
    //    {
    //        _canvasManager.SetSplashCanvasEnabled(enabled);
    //    }
    //}

    #endregion

    #region LEGACY

    public void ShowLevelSelector() => _sceneTransitionManager.ShowLevelSelector();
    public void LoadLevelScene(string sceneName) => _sceneTransitionManager.LoadLevelScene(sceneName);
    public void RestartLevel() => _sceneTransitionManager.RestartLevel();
    public void ShowConfirmationPanel(string sceneName) => _preGameUIManager.ShowConfirmationPanel(sceneName);
    public void ShowHidePreGameCanvas() => _canvasManager.ShowHidePreGameCanvas();
    public void ShowHideCreditsCanvas() => _canvasManager.ShowHideCreditsCanvas();
    public void ShowHideProfileCanvas() => _canvasManager.ShowHideProfileCanvas();
    public void SwitchHapticFeedback() => _isHapticFeedbackActive = !_isHapticFeedbackActive;
    public void ShowHidePauseCanvas() => TogglePauseOverlay();
    //public void ShowHideUserIconsCanvas() => _canvasManager.ShowHideUserIconsCanvas();
    //public void ShowHideUserNicknameEditCanvas() => _canvasManager.ShowHideUserNicknameEditCanvas();
    public void ShowHideResultsCanvas() => _canvasManager.ShowHideResultsCanvas();
    public void ShowHideLifeLostCanvas() => ShowLifeLostOverlay(LifeManager.Instance?.CurrentLives ?? 0);
    public void ShowHideDailyRewardCanvas() => _canvasManager.ShowHideDailyRewardCanvas();
    public void ShowHideNoLivesCanvas() => ShowNoLivesOverlay();
    //public void ShowLifeLostPanel() => _gameplayUIManager.ShowLifeLostPanel();
    //public void HideLifeLostPanel() => _gameplayUIManager.HideLifeLostPanel();
    public void UpdateLivesUI(int lives) => _gameplayUIManager.UpdateLivesUI(lives);
    public void ShowNoLivesPanel() => _gameplayUIManager.ShowNoLivesPanel();
    public void ShowHideStoreCanvas() => _canvasManager.ShowHideStoreCanvas();

    #endregion
}