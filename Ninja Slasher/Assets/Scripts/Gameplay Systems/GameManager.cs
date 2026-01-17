using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourSingleton<GameManager>
{
    [Header("Testing")]
    [SerializeField] private string[] testingScenes = { "TestScene" };

    private LevelStats currentStats;
    private bool _playerHasDied;
    private bool _levelStarted = false;
    private bool _levelEnded = false;

    public bool PlayerHasDied => _playerHasDied;

    public override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        GameEvents.OnLivesChanged += OnLivesChanged;

        _playerHasDied = false;
        _levelStarted = false;
        _levelEnded = false;

        if (!LifeManager.Instance.CanPlay())
        {
            GoToLevelSelection();
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        GameEvents.OnLevelCompleted += OnLevelCompleted;
        GameEvents.OnLevelFailed += OnLevelFailed;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelCompleted -= OnLevelCompleted;
        GameEvents.OnLevelFailed -= OnLevelFailed;
        GameEvents.OnLivesChanged -= OnLivesChanged;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("Level"))
        {
            if (!_levelStarted && LifeManager.Instance.CanPlay())
            {
                StartLevel();
            }
        }

        Debug.Log($"[LevelManager] Escena cargada: {scene.name}");
    }

    private void StartLevel()
    {
        if (_levelStarted || !LifeManager.Instance.CanPlay()) return;

        _playerHasDied = false;
        _levelEnded = false;

        _levelStarted = true;
        LifeManager.Instance.OnLevelStart();

        if (LevelSessionManager.Instance != null)
        {
            LevelSessionManager.Instance.StartLevel();
        }

        Debug.Log("[LevelManager] Nivel iniciado");
    }

    private void OnLevelCompleted(LevelStats stats)
    {
        if (IsTestingScene()) return;

        GameEvents.RaiseLevelEndedConsumePowerUps();

        if (_levelEnded)
        {
            Debug.LogWarning("[LevelManager] Nivel ya terminado, ignorando");
            return;
        }

        currentStats = stats;

        if (_levelStarted)
        {
            LifeManager.Instance.OnLevelCompleted();
            _levelStarted = false;
        }

        _levelEnded = true;

        int currentLevelId = GetCurrentLevelId();
        int starsEarned = 0;

        if (LevelSessionManager.Instance != null && LevelSessionManager.Instance.CurrentSession != null)
        {
            starsEarned = LevelSessionManager.Instance.CurrentSession.CurrentStats.enemiesDefeated > 0 ? 1 : 0;
        }

        Debug.Log($"[LevelManager] Nivel {currentLevelId} completado con {starsEarned} estrellas");

        StartCoroutine(HandleVictoryWithDelay());
    }

    private IEnumerator HandleVictoryWithDelay()
    {
        AudioManager.Instance.PlaySFX(SFXClip.UI_Victory);

        float soundDuration = AudioManager.Instance.GetSFXDuration(SFXClip.UI_Victory);

        if (soundDuration <= 0f)
        {
            soundDuration = 2.0f;
        }

        yield return new WaitForSeconds(soundDuration);

        UIManager.Instance.ShowHideResultsCanvas();
    }

    private void HandleLevelDefeat(string reason = "unknown")
    {
        if (_levelEnded)
        {
            Debug.LogWarning("[LevelManager] Nivel ya terminado, ignorando derrota");
            return;
        }

        _levelEnded = true;

        AudioManager.Instance.PlaySFX(SFXClip.UI_Defeat);

        if (_levelStarted && AnalyticsManager.Instance != null)
        {
            int currentLevelId = GetCurrentLevelId();
            float attemptTime = 0f;

            if (LevelSessionManager.Instance != null)
            {
                attemptTime = LevelSessionManager.Instance.GetTimeTaken();
            }

            AnalyticsManager.Instance.RecordLevelFailed(
                currentLevelId,
                reason,
                attemptTime
            );
        }

        if (_levelStarted)
        {
            LifeManager.Instance.UseLife();
            _levelStarted = false;
        }

        Debug.Log($"[LevelManager] Nivel fallado - Razón: {reason}");

        StartCoroutine(HandleDefeatUIWithDelay());
    }

    private IEnumerator HandleDefeatUIWithDelay()
    {
        float soundDuration = AudioManager.Instance.GetSFXDuration(SFXClip.UI_Defeat);

        if (soundDuration <= 0f)
        {
            soundDuration = 1.5f;
        }

        yield return new WaitForSeconds(soundDuration);

        if (LifeManager.Instance.GetRealLives() <= 0)
        {
            Debug.Log("[LevelManager] Sin vidas - volviendo a selección");
            GoToLevelSelection();
        }
        else if (LifeManager.Instance.CanPlay())
        {
            Debug.Log("[LevelManager] Mostrando panel de vida perdida");
            UIManager.Instance.ShowHideLifeLostCanvas();
        }
        else
        {
            Debug.Log("[LevelManager] Mostrando panel sin vidas");
            UIManager.Instance.ShowNoLivesPanel();
        }
    }

    public void TriggerLevelDefeat(string reason)
    {
        HandleLevelDefeat(reason);
    }

    public void OnLevelFailed()
    {
        OnLevelFailed("timeExpired");
    }

    private void OnLevelFailed(string reason)
    {
        HandleLevelDefeat(reason);
        GameEvents.RaiseLevelEndedConsumePowerUps();
    }

    public void OnPlayerLose()
    {
        _playerHasDied = true;
        HandleLevelDefeat("playerDeath");
    }

    private void OnLivesChanged(int newLives)
    {
        if (newLives > 0)
        {
            Debug.Log($"[LevelManager] Vidas actualizadas: {newLives}");
        }
    }

    public void GoToLevelSelection(bool confirmPendingDeduction = true)
    {
        if (AnalyticsManager.Instance != null)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            AnalyticsManager.Instance.RecordScreenTransition(currentScene, "LevelSelection");
        }

        if (confirmPendingDeduction && (_levelStarted || LifeManager.Instance.HasPendingDeduction()))
        {
            LifeManager.Instance.OnLevelExit();
            _levelStarted = false;
        }

        SaveManager.Instance.SaveData();
        SceneManager.sceneLoaded += HandleScreenflowLoaded;
        SceneManager.LoadScene("SplashScreen");
        AudioManager.Instance.PlayMusic(MusicClip.MainMenu, true);

        Debug.Log("[LevelManager] Volviendo a selección de nivel");
    }

    private void HandleScreenflowLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "SplashScreen") return;

        UIManager.Instance.ShowLevelSelector();
        SceneManager.sceneLoaded -= HandleScreenflowLoaded;
    }

    public void RestartLevel()
    {
        if (_levelStarted || LifeManager.Instance.HasPendingDeduction())
        {
            LifeManager.Instance.OnLevelExit();
            _levelStarted = false;
        }

        if (!LifeManager.Instance.CanPlay())
        {
            GoToLevelSelection();
            return;
        }

        _playerHasDied = false;
        _levelEnded = false;
        string currentScene = SceneManager.GetActiveScene().name;

        LifeManager.Instance.OnLevelStart();
        _levelStarted = true;

        SceneManager.LoadScene(currentScene);

        Debug.Log("[LevelManager] Reiniciando nivel");
    }

    private int GetCurrentLevelId()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName.Contains("Level"))
        {
            string levelNumber = sceneName.Replace("Level", "").Replace("_", "");
            if (int.TryParse(levelNumber, out int levelId))
            {
                return levelId;
            }
        }

        return 1;
    }

    private void OnDestroy()
    {
        GameEvents.OnLevelCompleted -= OnLevelCompleted;
        GameEvents.OnLevelFailed -= OnLevelFailed;
        GameEvents.OnLivesChanged -= OnLivesChanged;
    }

    private bool IsTestingScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        foreach (string testScene in testingScenes)
        {
            if (currentScene == testScene)
                return true;
        }

        return false;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // TO DO
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && _levelStarted)
        {
            LifeManager.Instance.OnLevelExit();
            _levelStarted = false;

            Debug.Log("[LevelManager] App perdió focus - vida deducida");
        }
    }
}