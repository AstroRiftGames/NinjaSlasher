using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public enum TutorialProgressState
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2
}

public class TutorialManager : MonoBehaviourSingleton<TutorialManager>
{
    private const string TutorialStepId = "panel";
    private const int TutorialStepIndex = 0;
    private const int TutorialStepsCount = 1;

    private TutorialUIOverlay _activePanelInstance;
    private string _activeTutorialId;
    private Coroutine _pendingSceneEvaluation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        var bootstrap = new GameObject(nameof(TutorialManager));
        bootstrap.AddComponent<TutorialManager>();
    }

    public override void Awake()
    {
        base.Awake();
    }

    private void OnEnable()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded += HandleSceneLoaded;
        GameEvents.OnLevelCompleted += HandleLevelCompleted;
        GameEvents.OnLevelFailed += HandleLevelFailed;
    }

    private void OnDisable()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        GameEvents.OnLevelCompleted -= HandleLevelCompleted;
        GameEvents.OnLevelFailed -= HandleLevelFailed;

        if (_pendingSceneEvaluation != null)
        {
            StopCoroutine(_pendingSceneEvaluation);
            _pendingSceneEvaluation = null;
        }

        HideActiveTutorial("managerDisabled", markAsCompleted: false);
    }

    public bool IsTutorialActive()
    {
        return _activePanelInstance != null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_pendingSceneEvaluation != null)
            StopCoroutine(_pendingSceneEvaluation);

        _pendingSceneEvaluation = StartCoroutine(EvaluateTutorialWhenReady(scene));
    }

    private IEnumerator EvaluateTutorialWhenReady(Scene scene)
    {
        HideActiveTutorial("sceneChanged", markAsCompleted: false);

        if (!scene.IsValid())
            yield break;

        for (int frame = 0; frame < 30; frame++)
        {
            if (!scene.IsValid() || SceneManager.GetActiveScene() != scene)
                yield break;

            LevelConfiguration levelConfiguration = ResolveLevelConfiguration(scene);
            if (levelConfiguration == null)
            {
                yield return null;
                continue;
            }

            if (UIManager.Instance == null || UIManager.Instance.GetTutorialOverlay() == null || !UIEvents.HasTutorialOverlayListener())
            {
                yield return null;
                continue;
            }

            yield return WaitForTransitionPresentation(scene);

            if (!scene.IsValid() || SceneManager.GetActiveScene() != scene)
                yield break;

            EvaluateTutorialForScene(scene, levelConfiguration);
            _pendingSceneEvaluation = null;
            yield break;
        }

        Debug.LogWarning($"[TutorialManager] No se pudo inicializar el tutorial para la escena '{scene.name}' porque UI o configuración no estuvieron listas a tiempo.");
        _pendingSceneEvaluation = null;
    }

    private IEnumerator WaitForTransitionPresentation(Scene scene)
    {
        while (scene.IsValid()
               && SceneManager.GetActiveScene() == scene
               && PauseController.Instance != null
               && PauseController.Instance.IsPauseSourceActive(PauseSource.Transition))
        {
            yield return null;
        }
    }

    private void EvaluateTutorialForScene(Scene scene, LevelConfiguration levelConfiguration)
    {
        if (!ShouldShowTutorials())
            return;

        if (levelConfiguration == null || levelConfiguration.tutorialPanelData == null)
            return;

        string tutorialId = BuildTutorialId(levelConfiguration.levelId);
        if (IsTutorialCompleted(tutorialId))
            return;

        TutorialUIOverlay panelView = ResolveScenePanelView();
        if (panelView == null)
        {
            Debug.LogWarning("[TutorialManager] No se encontró TutorialPanelView en la escena actual.");
            return;
        }

        _activePanelInstance = panelView;
        _activePanelInstance.Bind(levelConfiguration.tutorialPanelData);
        _activePanelInstance.Dismissed += HandlePanelDismissed;
        _activeTutorialId = tutorialId;

        SaveTutorialProgress((int)TutorialProgressState.InProgress);

        AnalyticsManager.Instance?.RecordTutorialStarted(_activeTutorialId, TutorialStepsCount);
        AnalyticsManager.Instance?.RecordTutorialStepStarted(_activeTutorialId, TutorialStepId, TutorialStepIndex, TutorialStepsCount);

        UIEvents.RequestShowTutorialOverlay();
    }

    private bool ShouldShowTutorials()
    {
        return GameConfigManager.Config == null || GameConfigManager.Config.enableTutorial;
    }

    private LevelConfiguration ResolveLevelConfiguration(Scene scene)
    {
        if (!scene.IsValid() || !scene.name.Contains("Level"))
            return null;

        if (LevelSessionManager.Instance?.CurrentSession?.Configuration != null)
            return LevelSessionManager.Instance.CurrentSession.Configuration;

        if (LevelConfigurationManager.Instance == null)
            return null;

        string cleanName = scene.name.Replace("Level_", "").Replace("Level", "");
        return int.TryParse(cleanName, out int levelId)
            ? LevelConfigurationManager.Instance.GetConfigurationForLevel(levelId)
            : null;
    }

    private TutorialUIOverlay ResolveScenePanelView()
    {
        return UIManager.Instance != null
            ? UIManager.Instance.GetTutorialOverlay()
            : null;
    }

    private void HandlePanelDismissed()
    {
        HideActiveTutorial("dismissed", markAsCompleted: true);
    }

    private void HandleLevelCompleted(LevelStats _)
    {
        HideActiveTutorial("levelCompleted", markAsCompleted: false);
    }

    private void HandleLevelFailed(LevelFailedContext _)
    {
        HideActiveTutorial("levelFailed", markAsCompleted: false);
    }

    private void HideActiveTutorial(string reason, bool markAsCompleted)
    {
        if (_activePanelInstance == null)
            return;

        _activePanelInstance.Dismissed -= HandlePanelDismissed;

        UIEvents.RequestHideTutorialOverlay();

        if (markAsCompleted)
        {
            AnalyticsManager.Instance?.RecordTutorialStepCompleted(_activeTutorialId, TutorialStepId, TutorialStepIndex, TutorialStepsCount);
            AnalyticsManager.Instance?.RecordTutorialCompleted(_activeTutorialId, TutorialStepsCount);
            SaveTutorialProgress((int)TutorialProgressState.Completed);
        }
        else
        {
            AnalyticsManager.Instance?.RecordTutorialAbandoned(_activeTutorialId, reason, TutorialStepId, TutorialStepIndex, TutorialStepsCount);
            SaveTutorialProgress((int)TutorialProgressState.InProgress);
        }

        _activePanelInstance = null;
        _activeTutorialId = null;
    }

    private string BuildTutorialId(int levelId)
    {
        return $"tutorial_level_{levelId}";
    }

    private bool IsTutorialCompleted(string tutorialId)
    {
        return SaveManager.Instance != null &&
               SaveManager.Instance.GetTutorialState(tutorialId) == (int)TutorialProgressState.Completed;
    }

    private void SaveTutorialProgress(int state)
    {
        if (SaveManager.Instance == null || string.IsNullOrWhiteSpace(_activeTutorialId))
            return;

        SaveManager.Instance.SaveTutorialProgress(_activeTutorialId, state, TutorialStepIndex);
    }
}
