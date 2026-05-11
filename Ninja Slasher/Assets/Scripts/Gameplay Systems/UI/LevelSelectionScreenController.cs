using System;
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
    
    private const string DailyAvailabilityParameterName = "IsAvailable";
    private Animator _dailyRewardButtonAnimator;
    private Animator _dailyWheelButtonAnimator;
    private LevelsScreen _levelsScreen;

    private void Awake()
    {
        Instance = this;
        ResolveLevelsScreen();
        ResolveLivesFeedbackReferences();
        CacheDailyButtons();
        StoreRewardFeedbackController.EnsureFor(this);
    }

    private void OnEnable()
    {
        Instance = this;
        ResolveLevelsScreen();
        ResolveLivesFeedbackReferences();
        CacheDailyButtons();
        StoreRewardFeedbackController.EnsureFor(this);
        RefreshAll();
        UpdateTotalStarsDisplay();
        RefreshLivesWidget();
        RefreshDailyButtonVisuals();
        RegisterButtonListeners();
        UpdateForegroundVisibility();

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

        GameEvents.OnRewardAvailabilityChanged -= OnRewardAvailabilityChanged;
        GameEvents.OnWheelAvailabilityChanged -= OnWheelAvailabilityChanged;

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

    private void Update()
    {
        RefreshLivesWidget();
        RefreshDailyButtonVisuals();
        UpdateForegroundVisibility();
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
        RefreshLivesWidget();
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
        Debug.Log($"[LevelSelectionScreenController] Area {areaId} unlocked.");
    }

    private void OnAreaUnlockAnimationComplete(AreaSectionController area)
    {
        // Refresca los botones de niveles tras completar la animación de nubes
        ButtonManager.Instance?.RefreshLevelProgression();
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
        SetDailyButtonAvailability(_dailyRewardButtonAnimator, isAvailable);
    }

    private void OnWheelAvailabilityChanged(bool isAvailable)
    {
        SetDailyButtonAvailability(_dailyWheelButtonAnimator, isAvailable);
    }

    private void UpdateForegroundVisibility()
    {
        if (_levelsScreen == null)
            return;

        bool wasVisible = _levelsScreen.IsForegroundVisible;
        _levelsScreen.RefreshForegroundVisibility("LevelSelectionScreenController.UpdateForegroundVisibility");

        if (!wasVisible && _levelsScreen.IsForegroundVisible)
        {
            RebindDailyButtonAnimators();
            RefreshDailyButtonVisuals();
        }
    }

    private void RefreshDailyButtonVisuals()
    {
        if (_dailyRewardButtonAnimator != null && DailyRewardSystem.Instance != null)
            SetDailyButtonAvailability(_dailyRewardButtonAnimator, DailyRewardSystem.Instance.CanClaimToday());

        if (_dailyWheelButtonAnimator != null && DailyWheelSystem.Instance != null)
            SetDailyButtonAvailability(_dailyWheelButtonAnimator, DailyWheelSystem.Instance.CanSpinToday());
    }

    private void RefreshLivesWidget()
    {
        LifeManager lifeManager = LifeManager.Instance;
        if (lifeManager == null || !lifeManager.IsInitialized)
            return;

        if (_livesAmountText != null)
            _livesAmountText.text = lifeManager.GetDisplayLives().ToString();

        bool hasUnlimitedLives = lifeManager.HasTimedUnlimitedLives;
        if (_livesTimerText != null)
        {
            if (hasUnlimitedLives)
            {
                _livesTimerText.text = FormatUnlimitedLivesTime(lifeManager.GetUnlimitedLivesRemainingTime());
            }
            else
            {
                TimeSpan nextLife = lifeManager.GetTimeToNextLife();
                _livesTimerText.text = nextLife.TotalSeconds > 0d
                    ? $"{nextLife.Minutes:D2}:{nextLife.Seconds:D2}"
                    : string.Empty;
            }
        }
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

    private void ResolveLevelsScreen()
    {
        if (_levelsScreen != null)
            return;

        _levelsScreen = GetComponent<LevelsScreen>();
        if (_levelsScreen == null)
            _levelsScreen = GetComponentInParent<LevelsScreen>(true);

        if (_levelsScreen == null)
            Debug.LogWarning("[LevelSelectionScreenController] LevelsScreen was not found. Foreground refresh requests will be ignored.");
    }
}
