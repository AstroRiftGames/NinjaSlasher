using System;
using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

public class LevelSelectionScreenController : MonoBehaviour
{
    public static LevelSelectionScreenController Instance { get; private set; }

    [Header("Areas")]
    [SerializeField] private AreaSectionController[] _areas;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _totalStarsText;
    [SerializeField] private RectTransform _livesWidgetRoot;
    [SerializeField] private TextMeshProUGUI _livesAmountText;
    [SerializeField] private TextMeshProUGUI _livesTimerText;
    [SerializeField] private UIPunchScaleFeedback _livesWidgetFeedback;
    [SerializeField] private UIPunchScaleFeedback _livesAmountFeedback;
    [SerializeField] private Button _dailyRewardButton;
    [SerializeField] private Button _storeButton;
    [SerializeField] private Button _dailyWheelButton;
    [SerializeField] private FirstTimeWelcomeController _firstTimeWelcomeController;
    
    private const string DailyAvailabilityParameterName = "IsAvailable";
    private static readonly WaitForSecondsRealtime PresenterTickDelay = new(1f);
    private Animator _dailyRewardButtonAnimator;
    private Animator _dailyWheelButtonAnimator;
    private ButtonManager _buttonManager;
    private Coroutine _presenterTickRoutine;
    private string _lastLivesAmountValue;
    private string _lastLivesTimerValue;
    private bool? _lastDailyRewardAvailability;
    private bool? _lastDailyWheelAvailability;

    private void Awake()
    {
        Instance = this;
        ResolveButtonManager();
        ResolveLivesFeedbackReferences();
        CacheDailyButtons();
        ResolveFirstTimeWelcomeController();
        StoreRewardFeedbackController.EnsureFor(this);
    }

    private void OnEnable()
    {
        Instance = this;
        ResolveButtonManager();
        ResolveLivesFeedbackReferences();
        CacheDailyButtons();
        ResolveFirstTimeWelcomeController();
        StoreRewardFeedbackController.EnsureFor(this);
        RefreshAll();
        UpdateTotalStarsDisplay();
        RefreshLivesWidget("OnEnable", force: true);
        RefreshDailyButtonVisuals("OnEnable", force: true);
        RegisterFirstTimeWelcomeListeners();
        ApplyFirstTimeWelcomeLevelButtonSuppression("OnEnable");
        RegisterButtonListeners();
        StartPresenterTick();

        GameEvents.OnLivesChanged += OnLivesChanged;
        GameEvents.OnRewardAvailabilityChanged += OnRewardAvailabilityChanged;
        GameEvents.OnWheelAvailabilityChanged += OnWheelAvailabilityChanged;

        if (LevelProgressionManager.Instance != null)
        {
            LevelProgressionManager.Instance.OnNewAreaUnlocked += HandleNewAreaUnlocked;
            LevelProgressionManager.Instance.OnProgressionUpdated += UpdateTotalStarsDisplay;
        }

        foreach (var area in _areas)
        {
            if (area != null)
                area.OnUnlocked += OnAreaUnlockAnimationComplete;
        }
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;

        StopPresenterTick();

        GameEvents.OnLivesChanged -= OnLivesChanged;
        GameEvents.OnRewardAvailabilityChanged -= OnRewardAvailabilityChanged;
        GameEvents.OnWheelAvailabilityChanged -= OnWheelAvailabilityChanged;
        SaveManager.OnDataLoaded -= OnSaveDataLoaded;
        UnregisterFirstTimeWelcomeListeners();

        if (LevelProgressionManager.Instance != null)
        {
            LevelProgressionManager.Instance.OnNewAreaUnlocked -= HandleNewAreaUnlocked;
            LevelProgressionManager.Instance.OnProgressionUpdated -= UpdateTotalStarsDisplay;
        }

        foreach (var area in _areas)
        {
            if (area != null)
                area.OnUnlocked -= OnAreaUnlockAnimationComplete;
        }

        UnregisterButtonListeners();
    }

    public RectTransform GetUnlimitedLivesFeedbackTarget()
    {
        if (_livesWidgetRoot != null)
            return _livesWidgetRoot;

        if (_livesTimerText != null)
            return _livesTimerText.rectTransform;

        if (_livesAmountText != null)
            return _livesAmountText.rectTransform;

        return null;
    }

    public Canvas GetRootCanvas()
    {
        return GetComponentInParent<Canvas>();
    }

    public void PlayUnlimitedLivesArrivalFeedback()
    {
        RefreshLivesWidget("PlayUnlimitedLivesArrivalFeedback", force: true);
        ResolveLivesFeedbackReferences();

        RectTransform target = GetUnlimitedLivesFeedbackTarget();
        if (target != null)
        {
            if (_livesWidgetFeedback != null)
            {
                _livesWidgetFeedback.Play();
            }
            else
            {
                DOTween.Kill(target);
                target.DOPunchScale(new Vector3(0.26f, 0.26f, 0f), 0.32f, vibrato: 1, elasticity: 0.45f)
                    .SetUpdate(true);
            }
        }

        if (_livesAmountText != null)
        {
            if (_livesAmountFeedback != null)
            {
                _livesAmountFeedback.Play();
            }
            else
            {
                DOTween.Kill(_livesAmountText.rectTransform);
                _livesAmountText.rectTransform.DOPunchScale(new Vector3(0.16f, 0.16f, 0f), 0.24f, vibrato: 1, elasticity: 0.4f)
                    .SetUpdate(true);
            }
        }
    }

    private void RefreshAll()
    {
        foreach (var area in _areas)
        {
            if (area != null)
                area.Refresh();
        }
    }

    private void UpdateTotalStarsDisplay()
    {
        if (_totalStarsText == null) return;

        // Obtiene las estrellas totales acumuladas del jugador
        var (_, _, totalStars) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);
        _totalStarsText.text = totalStars.ToString();
    }

    private void HandleNewAreaUnlocked(int areaId)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LevelSelectionScreenController] Area {areaId} unlocked.");
#endif
    }

    public bool TryPlayPendingAreaUnlockFeedback()
    {
        if (_areas == null)
            return false;

        for (int i = 0; i < _areas.Length; i++)
        {
            AreaSectionController area = _areas[i];
            if (area != null && area.TryPlayPendingUnlockFeedback())
                return true;
        }

        return false;
    }

    private void OnAreaUnlockAnimationComplete(AreaSectionController area)
    {
        // Refresca los botones de niveles tras completar la animación de nubes
        if (_buttonManager == null)
        {
            Debug.LogWarning("[LevelSelectionScreenController] ButtonManager was not found. Level progression refresh was skipped.");
            return;
        }

        _buttonManager.RefreshLevelProgression();
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Refresh Todas las Áreas")]
    private void ContextMenuRefreshAll() => RefreshAll();
#endif

    private void CacheDailyButtons()
    {
        if (_dailyRewardButtonAnimator == null && _dailyRewardButton != null)
            _dailyRewardButtonAnimator = _dailyRewardButton.GetComponent<Animator>();

        if (_dailyWheelButtonAnimator == null && _dailyWheelButton != null)
            _dailyWheelButtonAnimator = _dailyWheelButton.GetComponent<Animator>();
    }

    private void RegisterButtonListeners()
    {
        _dailyRewardButton?.onClick.RemoveListener(OnDailyRewardButtonClicked);
        _dailyRewardButton?.onClick.AddListener(OnDailyRewardButtonClicked);

        _storeButton?.onClick.RemoveListener(OnStoreButtonClicked);
        _storeButton?.onClick.AddListener(OnStoreButtonClicked);

        _dailyWheelButton?.onClick.RemoveListener(OnDailyWheelButtonClicked);
        _dailyWheelButton?.onClick.AddListener(OnDailyWheelButtonClicked);
    }

    private void UnregisterButtonListeners()
    {
        _dailyRewardButton?.onClick.RemoveListener(OnDailyRewardButtonClicked);
        _storeButton?.onClick.RemoveListener(OnStoreButtonClicked);
        _dailyWheelButton?.onClick.RemoveListener(OnDailyWheelButtonClicked);
    }

    private void OnDailyRewardButtonClicked()
    {
        UIEvents.RequestShowDailyRewardModal();
    }

    private void OnDailyWheelButtonClicked()
    {
        UIEvents.RequestShowDailyWheelModal();
    }

    private void OnStoreButtonClicked()
    {
        UIEvents.RequestShowStoreModal();
    }

    private void OnRewardAvailabilityChanged(bool isAvailable)
    {
        ApplyDailyRewardAvailability(isAvailable, "GameEvents.OnRewardAvailabilityChanged", force: true);
    }

    private void OnWheelAvailabilityChanged(bool isAvailable)
    {
        ApplyDailyWheelAvailability(isAvailable, "GameEvents.OnWheelAvailabilityChanged", force: true);
    }

    private void OnSaveDataLoaded(GameData _)
    {
        ApplyFirstTimeWelcomeLevelButtonSuppression("SaveManager.OnDataLoaded");
    }

    private void OnLivesChanged(int lives)
    {
        RefreshLivesWidget("GameEvents.OnLivesChanged", force: true);
    }

    public void OnForegroundShown(string reason = null)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LevelSelectionScreenController] Presenter -> ForegroundShown | Reason={reason ?? "Unspecified"}");
#endif
        RebindDailyButtonAnimators();
        RefreshLivesWidget($"OnForegroundShown:{reason ?? "Unspecified"}", force: true);
        RefreshDailyButtonVisuals($"OnForegroundShown:{reason ?? "Unspecified"}", force: true);
        NotifyFirstTimeWelcomeScreenReady();
    }

    public void OnForegroundHidden(string reason = null)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LevelSelectionScreenController] Presenter -> ForegroundHidden | Reason={reason ?? "Unspecified"}");
#endif
    }

    private void RefreshDailyButtonVisuals(string reason, bool force = false)
    {
        if (DailyRewardSystem.Instance != null)
            ApplyDailyRewardAvailability(DailyRewardSystem.Instance.GetAvailabilitySnapshot().IsAvailable, reason, force);

        if (DailyWheelSystem.Instance != null)
            ApplyDailyWheelAvailability(DailyWheelSystem.Instance.GetAvailabilitySnapshot().IsAvailable, reason, force);
    }

    private void RefreshLivesWidget(string reason, bool force = false)
    {
        LifeManager lifeManager = LifeManager.Instance;
        if (lifeManager == null || !lifeManager.IsInitialized)
            return;

        string newLivesAmount = lifeManager.GetDisplayLives().ToString();
        string newTimerValue = string.Empty;
        bool hasUnlimitedLives = lifeManager.HasTimedUnlimitedLives;

        if (hasUnlimitedLives)
        {
            newTimerValue = FormatUnlimitedLivesTime(lifeManager.GetUnlimitedLivesRemainingTime());
        }
        else
        {
            TimeSpan nextLife = lifeManager.GetTimeToNextLife();
            newTimerValue = nextLife.TotalSeconds > 0d
                ? $"{nextLife.Minutes:D2}:{nextLife.Seconds:D2}"
                : string.Empty;
        }

        bool amountChanged = force || !string.Equals(_lastLivesAmountValue, newLivesAmount, StringComparison.Ordinal);
        bool timerChanged = force || !string.Equals(_lastLivesTimerValue, newTimerValue, StringComparison.Ordinal);

        if (_livesAmountText != null)
        {
            if (amountChanged)
                _livesAmountText.text = newLivesAmount;
        }

        if (_livesTimerText != null)
        {
            if (timerChanged)
                _livesTimerText.text = newTimerValue;
        }

        _lastLivesAmountValue = newLivesAmount;
        _lastLivesTimerValue = newTimerValue;
    }

    private string FormatUnlimitedLivesTime(TimeSpan remaining)
    {
        if (remaining.TotalHours >= 1d)
            return $"{Mathf.FloorToInt((float)remaining.TotalHours):D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";

        return $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
    }

    private void ResolveLivesFeedbackReferences()
    {
        RectTransform target = GetUnlimitedLivesFeedbackTarget();
        if (_livesWidgetFeedback == null && target != null)
            _livesWidgetFeedback = GetOrCreateFeedback(target, 0.32f, new Vector3(0.26f, 0.26f, 0f), 0.45f);

        if (_livesAmountFeedback == null && _livesAmountText != null)
            _livesAmountFeedback = GetOrCreateFeedback(_livesAmountText.rectTransform, 0.24f, new Vector3(0.16f, 0.16f, 0f), 0.4f);
    }

    private UIPunchScaleFeedback GetOrCreateFeedback(RectTransform target, float punchDuration, Vector3 punchStrength, float elasticity)
    {
        UIPunchScaleFeedback feedback = target.GetComponent<UIPunchScaleFeedback>();
        if (feedback == null)
            feedback = target.gameObject.AddComponent<UIPunchScaleFeedback>();

        feedback.Configure(
            useFade: false,
            hideGameObjectOnComplete: false,
            activateGameObjectOnPlay: false,
            useUnscaledTime: true,
            scaleInDuration: 0f,
            punchDuration: punchDuration,
            visibleDuration: 0f,
            fadeDuration: 0f,
            initialScaleMultiplier: 1f,
            punchStrength: punchStrength,
            vibrato: 1,
            elasticity: elasticity);

        feedback.ResetImmediate();
        return feedback;
    }

    private void RebindDailyButtonAnimators()
    {
        RebindAnimator(_dailyRewardButtonAnimator);
        RebindAnimator(_dailyWheelButtonAnimator);
    }

    private static void RebindAnimator(Animator animator)
    {
        if (animator == null || !animator.isActiveAndEnabled)
            return;

        animator.Rebind();
        animator.Update(0f);
    }

    private static void SetDailyButtonAvailability(Animator animator, bool isAvailable)
    {
        if (animator == null || !animator.isActiveAndEnabled)
            return;

        if (!HasBoolParameter(animator, DailyAvailabilityParameterName))
            return;

        if (animator.GetBool(DailyAvailabilityParameterName) != isAvailable)
            animator.SetBool(DailyAvailabilityParameterName, isAvailable);
    }

    private static bool HasBoolParameter(Animator animator, string parameterName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Bool
                && parameters[i].name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveButtonManager()
    {
        if (_buttonManager != null)
            return;

        _buttonManager = GetComponentInParent<ButtonManager>(true);

        if (_buttonManager == null)
            Debug.LogWarning("[LevelSelectionScreenController] ButtonManager was not found.");
    }

    private void ResolveFirstTimeWelcomeController()
    {
        if (_firstTimeWelcomeController != null)
            return;

        _firstTimeWelcomeController = GetComponent<FirstTimeWelcomeController>();
        if (_firstTimeWelcomeController == null)
            Debug.LogWarning("[LevelSelectionScreenController] FirstTimeWelcomeController is not assigned.");
    }

    private void RegisterFirstTimeWelcomeListeners()
    {
        SaveManager.OnDataLoaded -= OnSaveDataLoaded;
        SaveManager.OnDataLoaded += OnSaveDataLoaded;

        ResolveFirstTimeWelcomeController();
        if (_firstTimeWelcomeController == null)
            return;

        _firstTimeWelcomeController.LevelSelectionInputBlockChanged -= OnFirstTimeWelcomeInputBlockChanged;
        _firstTimeWelcomeController.LevelSelectionInputBlockChanged += OnFirstTimeWelcomeInputBlockChanged;
    }

    private void UnregisterFirstTimeWelcomeListeners()
    {
        if (_firstTimeWelcomeController == null)
            return;

        _firstTimeWelcomeController.LevelSelectionInputBlockChanged -= OnFirstTimeWelcomeInputBlockChanged;
    }

    private void OnFirstTimeWelcomeInputBlockChanged(bool shouldBlock)
    {
        SetLevelButtonInputSuppressed(shouldBlock, "FirstTimeWelcomeController");
    }

    private void ApplyFirstTimeWelcomeLevelButtonSuppression(string reason)
    {
        GameData data = SaveManager.Instance != null && SaveManager.Instance.IsDataLoaded
            ? SaveManager.Instance.GetGameData()
            : null;

        bool shouldBlock = FirstTimeWelcomeSaveState.IsWelcomePendingForCurrentEntry(data);
        SetLevelButtonInputSuppressed(shouldBlock, reason);
    }

    private void SetLevelButtonInputSuppressed(bool suppressed, string reason)
    {
        ResolveButtonManager();
        _buttonManager?.SetLevelButtonInputSuppressed(suppressed);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LevelSelectionScreenController] LevelButtonsInputSuppressed -> {suppressed} | Reason={reason}");
#endif
    }

    public void NotifyFirstTimeWelcomeScreenReady()
    {
        ResolveFirstTimeWelcomeController();
        _firstTimeWelcomeController?.NotifyScreenReady();
    }

    private void StartPresenterTick()
    {
        if (_presenterTickRoutine != null)
            return;

        _presenterTickRoutine = StartCoroutine(PresenterTickLoop());
    }

    private void StopPresenterTick()
    {
        if (_presenterTickRoutine == null)
            return;

        StopCoroutine(_presenterTickRoutine);
        _presenterTickRoutine = null;
    }

    private IEnumerator PresenterTickLoop()
    {
        while (enabled)
        {
            yield return PresenterTickDelay;

            RefreshLivesWidget("PresenterTick");
            RefreshDailyButtonVisuals("PresenterTick");
        }
    }

    private void ApplyDailyRewardAvailability(bool isAvailable, string reason, bool force = false)
    {
        bool changed = force || !_lastDailyRewardAvailability.HasValue || _lastDailyRewardAvailability.Value != isAvailable;
        if (!changed)
            return;

        SetDailyButtonAvailability(_dailyRewardButtonAnimator, isAvailable);
        _lastDailyRewardAvailability = isAvailable;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LevelSelectionScreenController] DailyRewardAvailability -> {isAvailable} | Reason={reason}");
#endif
    }

    private void ApplyDailyWheelAvailability(bool isAvailable, string reason, bool force = false)
    {
        bool changed = force || !_lastDailyWheelAvailability.HasValue || _lastDailyWheelAvailability.Value != isAvailable;
        if (!changed)
            return;

        SetDailyButtonAvailability(_dailyWheelButtonAnimator, isAvailable);
        _lastDailyWheelAvailability = isAvailable;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LevelSelectionScreenController] DailyWheelAvailability -> {isAvailable} | Reason={reason}");
#endif
    }
}
