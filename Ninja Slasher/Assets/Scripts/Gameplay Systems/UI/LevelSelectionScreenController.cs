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
    [SerializeField] private Button _dailyWheelButton;
    [SerializeField] private GameObject _infoRoot;
    [SerializeField] private GameObject _buttonsRoot;

    private void Awake()
    {
        Instance = this;
        CacheLivesWidgetReferences();
        CacheDailyWheelButton();
        StoreRewardFeedbackController.EnsureFor(this);
    }

    private void OnEnable()
    {
        Instance = this;
        CacheLivesWidgetReferences();
        CacheDailyWheelButton();
        StoreRewardFeedbackController.EnsureFor(this);
        RefreshAll();
        UpdateTotalStarsDisplay();
        RefreshLivesWidget();
        RegisterButtonListeners();
        UpdateForegroundVisibility();

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

        RectTransform target = GetUnlimitedLivesFeedbackTarget();
        if (target != null)
        {
            DOTween.Kill(target);
            target.DOPunchScale(new Vector3(0.26f, 0.26f, 0f), 0.32f, vibrato: 1, elasticity: 0.45f)
                .SetUpdate(true);
        }

        if (_livesAmountText != null)
        {
            DOTween.Kill(_livesAmountText.rectTransform);
            _livesAmountText.rectTransform.DOPunchScale(new Vector3(0.16f, 0.16f, 0f), 0.24f, vibrato: 1, elasticity: 0.4f)
                .SetUpdate(true);
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

    private void CacheLivesWidgetReferences()
    {
        if (_infoRoot == null)
        {
            Transform infoTransform = transform.Find("Info");
            if (infoTransform != null)
                _infoRoot = infoTransform.gameObject;
        }

        if (_buttonsRoot == null)
        {
            Transform buttonsTransform = transform.Find("Buttons");
            if (buttonsTransform != null)
                _buttonsRoot = buttonsTransform.gameObject;
        }

        if (_livesWidgetRoot == null)
            _livesWidgetRoot = FindRectTransformByName("Lives");

        if (_livesAmountText == null)
            _livesAmountText = FindTextByName("LivesAmount");

        if (_livesTimerText == null)
            _livesTimerText = FindTextByName("CounterText");
    }

    private void CacheDailyWheelButton()
    {
        if (_dailyWheelButton != null)
            return;

        RectTransform buttonRect = FindRectTransformByName("DailyWheelButton");
        if (buttonRect != null)
            _dailyWheelButton = buttonRect.GetComponent<Button>();
    }

    private void RegisterButtonListeners()
    {
        if (_dailyWheelButton == null)
            return;

        _dailyWheelButton.onClick.RemoveListener(OnDailyWheelButtonClicked);
        _dailyWheelButton.onClick.AddListener(OnDailyWheelButtonClicked);
    }

    private void UnregisterButtonListeners()
    {
        if (_dailyWheelButton == null)
            return;

        _dailyWheelButton.onClick.RemoveListener(OnDailyWheelButtonClicked);
    }

    private void OnDailyWheelButtonClicked()
    {
        UIEvents.RequestShowDailyWheelModal();
    }

    private void UpdateForegroundVisibility()
    {
        bool shouldShow = true;

        if (UIManager.Instance != null)
            shouldShow = !UIManager.Instance.HasBlockingPanelForLevelSelection();

        if (DailyStartupSequence.IsSequenceRunning)
            shouldShow = false;

        if (_infoRoot != null && _infoRoot.activeSelf != shouldShow)
            _infoRoot.SetActive(shouldShow);

        if (_buttonsRoot != null && _buttonsRoot.activeSelf != shouldShow)
            _buttonsRoot.SetActive(shouldShow);
    }

    private RectTransform FindRectTransformByName(string objectName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (Transform child in children)
        {
            if (child.name == objectName)
                return child as RectTransform;
        }

        return null;
    }

    private TextMeshProUGUI FindTextByName(string objectName)
    {
        RectTransform rect = FindRectTransformByName(objectName);
        return rect != null ? rect.GetComponent<TextMeshProUGUI>() : null;
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
}
