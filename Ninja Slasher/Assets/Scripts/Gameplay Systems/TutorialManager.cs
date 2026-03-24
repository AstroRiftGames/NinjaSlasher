using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TutorialProgressState
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("INDICATORS")]
    [SerializeField] private GameObject handAnimation;
    [SerializeField] private GameObject handAnimationParry;

    [Header("REFERENCES")]
    [SerializeField] private Controller playerController;

    [Header("DATA")]
    [SerializeField] private TutorialDefinition tutorialDefinition;
    [SerializeField] private TutorialSceneBinding[] sceneBindings;

    [Header("SETTINGS")]
    [SerializeField] private string tutorialId;
    private bool SkipTutorial => !GameConfigManager.Config.enableTutorial;

    private TutorialDefinition _runtimeDefinition;
    private TutorialDisplayController _displayController;
    private bool _tutorialActive;
    private int _currentStepIndex;
    private bool _currentStepArmed;
    private Coroutine _activeStepCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnDashStarted += HandleDashStarted;
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
        GameEvents.OnComboUpdated += HandleComboUpdated;
        GameEvents.OnParrySuccessful += HandleParrySuccessful;
        GameEvents.OnLevelCompleted += HandleLevelCompleted;
        GameEvents.OnLevelFailed += HandleLevelFailed;
    }

    private void OnDisable()
    {
        GameEvents.OnDashStarted -= HandleDashStarted;
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        GameEvents.OnComboUpdated -= HandleComboUpdated;
        GameEvents.OnParrySuccessful -= HandleParrySuccessful;
        GameEvents.OnLevelCompleted -= HandleLevelCompleted;
        GameEvents.OnLevelFailed -= HandleLevelFailed;

        CleanupRuntimeState();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        BuildRuntimeModel();

        if (SkipTutorial)
        {
            DisableTutorial("skipped");
            return;
        }

        if (IsTutorialCompleted())
        {
            DisableTutorial("alreadyCompleted");
            return;
        }

        StartTutorial();
    }

    public void StartTutorial()
    {
        if (_runtimeDefinition == null || _runtimeDefinition.steps == null || _runtimeDefinition.steps.Count == 0)
        {
            DisableTutorial("noDefinition");
            return;
        }

        bool isFreshStart = SaveManager.Instance == null ||
                            SaveManager.Instance.GetTutorialState(ResolvedTutorialId) != (int)TutorialProgressState.InProgress;

        _tutorialActive = true;
        _currentStepIndex = Mathf.Clamp(GetSavedStepIndex(), 0, _runtimeDefinition.steps.Count - 1);
        _currentStepArmed = false;

        CleanupCoroutines();

        if (playerController != null)
        {
            //playerController.SetInputEnabled(false);
        }

        _displayController.HideAll();
        SaveCurrentProgress();

        if (isFreshStart)
            AnalyticsManager.Instance?.RecordTutorialStarted(ResolvedTutorialId, _runtimeDefinition.steps.Count);

        ShowCurrentStep();
    }

    public bool IsTutorialActive()
    {
        return _tutorialActive;
    }

    public void OnDashPerformed()
    {
        TryCompleteCurrentStep(TutorialCompletionTrigger.DashStarted);
    }

    public void OnEnemyKilled()
    {
        TryCompleteCurrentStep(TutorialCompletionTrigger.EnemyKilled);
    }

    public void OnComboPerformed()
    {
        TryCompleteCurrentStep(TutorialCompletionTrigger.ComboUpdated);
    }

    public void OnParryPerformed()
    {
        TryCompleteCurrentStep(TutorialCompletionTrigger.ParrySuccessful);
    }

    public void DisableTutorial(string reason = "")
    {
        if (_tutorialActive)
        {
            var abandonedStep = GetCurrentStep();
            AnalyticsManager.Instance?.RecordTutorialAbandoned(
                ResolvedTutorialId,
                reason,
                abandonedStep?.stepId ?? "",
                _currentStepIndex,
                _runtimeDefinition?.steps?.Count ?? 0);
        }

        _tutorialActive = false;
        SaveCurrentProgress();
        CleanupRuntimeState();
    }

    private void BuildRuntimeModel()
    {
        _runtimeDefinition = tutorialDefinition;
        _displayController = new TutorialDisplayController(
            BuildBindings(),
            handAnimation,
            handAnimationParry);
    }

    private Dictionary<string, GameObject[]> BuildBindings()
    {
        var bindings = new Dictionary<string, GameObject[]>();

        if (sceneBindings != null)
        {
            for (int i = 0; i < sceneBindings.Length; i++)
            {
                TutorialSceneBinding binding = sceneBindings[i];
                if (binding == null || string.IsNullOrWhiteSpace(binding.stepId))
                    continue;

                bindings[binding.stepId] = binding.textObjects ?? System.Array.Empty<GameObject>();
            }
        }

        return bindings;
    }

    private void ShowCurrentStep()
    {
        TutorialStepDefinition step = GetCurrentStep();
        if (step == null)
        {
            CompleteTutorial();
            return;
        }

        _displayController.ShowStep(step);

        AnalyticsManager.Instance?.RecordTutorialStepStarted(
            ResolvedTutorialId,
            step.stepId,
            _currentStepIndex,
            _runtimeDefinition?.steps?.Count ?? 0);

        ArmCurrentStep(step);
    }

    private void ArmCurrentStep(TutorialStepDefinition step)
    {
        _currentStepArmed = false;
        CleanupCoroutines();

        if (step.completionTrigger == TutorialCompletionTrigger.AutoAdvance)
        {
            _activeStepCoroutine = StartCoroutine(AutoAdvanceAfterDelay(step.autoAdvanceDelay));
            return;
        }

        if (step.armingDelay > 0f)
        {
            _activeStepCoroutine = StartCoroutine(ArmAfterDelay(step.armingDelay));
            return;
        }

        _currentStepArmed = true;
    }

    private IEnumerator ArmAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        _currentStepArmed = true;
        _activeStepCoroutine = null;
    }

    private IEnumerator AutoAdvanceAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        _activeStepCoroutine = null;
        AdvanceToNextStep();
    }

    private IEnumerator CompleteTriggeredStepAfterDelay(float delay, TutorialStepDefinition step)
    {
        yield return new WaitForSecondsRealtime(delay);
        _activeStepCoroutine = null;

        if (step.handIndicator == TutorialHandIndicator.Dash)
            _displayController.HideDashIndicator();

        AdvanceToNextStep();
    }

    private void TryCompleteCurrentStep(TutorialCompletionTrigger trigger)
    {
        if (!_tutorialActive || !_currentStepArmed)
            return;

        TutorialStepDefinition step = GetCurrentStep();
        if (step == null || step.completionTrigger != trigger)
            return;

        _currentStepArmed = false;
        CleanupCoroutines();

        if (step.triggerCompletionDelay > 0f)
        {
            _activeStepCoroutine = StartCoroutine(CompleteTriggeredStepAfterDelay(step.triggerCompletionDelay, step));
            return;
        }

        if (step.handIndicator == TutorialHandIndicator.Dash)
            _displayController.HideDashIndicator();

        AdvanceToNextStep();
    }

    private void AdvanceToNextStep()
    {
        if (!_tutorialActive)
            return;

        var completedStep = GetCurrentStep();
        _currentStepIndex++;

        if (completedStep != null)
        {
            AnalyticsManager.Instance?.RecordTutorialStepCompleted(
                ResolvedTutorialId,
                completedStep.stepId,
                _currentStepIndex - 1,
                _runtimeDefinition?.steps?.Count ?? 0);
        }

        if (_runtimeDefinition == null || _currentStepIndex >= _runtimeDefinition.steps.Count)
        {
            CompleteTutorial();
            return;
        }

        SaveCurrentProgress();
        ShowCurrentStep();
    }

    private void CompleteTutorial()
    {
        _tutorialActive = false;
        SaveCompletedProgress();

        AnalyticsManager.Instance?.RecordTutorialCompleted(
            ResolvedTutorialId,
            _runtimeDefinition?.steps?.Count ?? 0);

        CleanupRuntimeState();
    }

    private void CleanupRuntimeState()
    {
        CleanupCoroutines();
        _currentStepArmed = false;

        if (_displayController != null)
            _displayController.HideAll();

        if (playerController != null)
        {
            //playerController.SetInputEnabled(true);
        }
    }

    private void CleanupCoroutines()
    {
        if (_activeStepCoroutine != null)
        {
            StopCoroutine(_activeStepCoroutine);
            _activeStepCoroutine = null;
        }
    }

    private TutorialStepDefinition GetCurrentStep()
    {
        if (_runtimeDefinition == null || _runtimeDefinition.steps == null)
            return null;

        if (_currentStepIndex < 0 || _currentStepIndex >= _runtimeDefinition.steps.Count)
            return null;

        return _runtimeDefinition.steps[_currentStepIndex];
    }

    private void HandleDashStarted()
    {
        OnDashPerformed();
    }

    private void HandleEnemyKilled(Vector3 _)
    {
        OnEnemyKilled();
    }

    private void HandleComboUpdated(int _, Vector3 __)
    {
        OnComboPerformed();
    }

    private void HandleParrySuccessful()
    {
        OnParryPerformed();
    }

    private void HandleLevelCompleted(LevelStats _)
    {
        DisableTutorial("levelCompleted");
    }

    private void HandleLevelFailed(LevelFailedContext _)
    {
        DisableTutorial("levelFailed");
    }

    private string ResolvedTutorialId
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(tutorialId))
                return tutorialId;

            if (tutorialDefinition != null && !string.IsNullOrWhiteSpace(tutorialDefinition.tutorialId))
                return tutorialDefinition.tutorialId;

            return string.Empty;
        }
    }

    private bool IsTutorialCompleted()
    {
        return SaveManager.Instance != null &&
               SaveManager.Instance.GetTutorialState(ResolvedTutorialId) == (int)TutorialProgressState.Completed;
    }

    private int GetSavedStepIndex()
    {
        if (SaveManager.Instance == null)
            return 0;

        if (SaveManager.Instance.GetTutorialState(ResolvedTutorialId) != (int)TutorialProgressState.InProgress)
            return 0;

        return SaveManager.Instance.GetTutorialStepIndex(ResolvedTutorialId);
    }

    private void SaveCurrentProgress()
    {
        if (SaveManager.Instance == null || string.IsNullOrWhiteSpace(ResolvedTutorialId))
            return;

        int state = _tutorialActive
            ? (int)TutorialProgressState.InProgress
            : SaveManager.Instance.GetTutorialState(ResolvedTutorialId);

        if (!_tutorialActive &&
            state == (int)TutorialProgressState.NotStarted &&
            _currentStepIndex == 0)
        {
            return;
        }

        SaveManager.Instance.SaveTutorialProgress(ResolvedTutorialId, state, _currentStepIndex);
    }

    private void SaveCompletedProgress()
    {
        if (SaveManager.Instance == null || string.IsNullOrWhiteSpace(ResolvedTutorialId))
            return;

        SaveManager.Instance.SaveTutorialProgress(
            ResolvedTutorialId,
            (int)TutorialProgressState.Completed,
            _currentStepIndex);
    }

}
