using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NoLivesModal : UIModalBase
{
    [Header("No Lives UI")]
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _claimLifeButton;

    private bool? _lastClaimLifeButtonVisible;
    private string _lastRecoveryState;
    private bool _suppressAbandonOnHide;
    private bool _isClaimLifeFlowInProgress;

    protected override void Awake()
    {
        base.Awake();
        SetupButtons();
    }

    private void OnEnable()
    {
        GameEvents.OnLivesChanged += OnLivesChanged;

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnRewardedAdReadinessChanged += UpdateButtons;
            AdsManager.Instance.OnRewardedAdFlowCompleted += OnRewardedAdFlowCompleted;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEvents.OnLivesChanged -= OnLivesChanged;

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnRewardedAdReadinessChanged -= UpdateButtons;
            AdsManager.Instance.OnRewardedAdFlowCompleted -= OnRewardedAdFlowCompleted;
        }
    }

    private void SetupButtons()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(OnCloseRequested);

        if (_claimLifeButton != null)
            _claimLifeButton.onClick.AddListener(OnClaimLifeClicked);
    }

    private void Update()
    {
        if (!_isVisible) return;

        UpdateTimer();
    }

    protected override void OnShown()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[NoLivesModal] No lives available | realLives={LifeManager.Instance?.GetRealLives() ?? -1} | displayLives={LifeManager.Instance?.GetDisplayLives() ?? -1} | rewarded={AdsManager.Instance?.GetRewardedAvailabilityReason() ?? "ads_manager_missing"}");
#endif

        UpdateMessage();
        UpdateTimer();
        UpdateButtons();
    }

    protected override void OnHidden()
    {
        if (_suppressAbandonOnHide)
        {
            _suppressAbandonOnHide = false;
            return;
        }

        if (_isClaimLifeFlowInProgress || (AdsManager.Instance != null && AdsManager.Instance.IsRewardedAdFlowInProgress("extra_life")))
            return;

        if (!LifeManager.Instance.CanPlay())
        {
            LifeManager.Instance?.NotifyLifeWallAbandoned();
            UIEvents.RaiseQuitToMenuPressed();
        }
    }

    private void UpdateMessage()
    {
        if (_messageText == null) return;

        int currentLives = LifeManager.Instance?.CurrentLives ?? 0;
        int maxLives = GameConfigManager.Config?.maxLives ?? 5;

        _messageText.text = $"Sin vidas disponibles\n{currentLives}/{maxLives}";
    }

    private void UpdateTimer()
    {
        if (_timerText == null) return;

        if (LifeManager.Instance == null)
        {
            _timerText.text = "--:--";
            return;
        }

        TimeSpan timeUntilNextLife = LifeManager.Instance.GetTimeToNextLife();

        if (timeUntilNextLife.TotalSeconds <= 0)
        {
            _timerText.text = "Vida disponible";
            UpdateButtons();
            return;
        }

        int minutes = timeUntilNextLife.Minutes;
        int seconds = timeUntilNextLife.Seconds;

        _timerText.text = $"Próxima vida en: {minutes:00}:{seconds:00}";
    }

    private void UpdateButtons()
    {
        bool hasLives = LifeManager.Instance?.CanPlay() ?? false;
        bool canWatchAd = CanWatchAdForRecovery();
        bool claimVisible = hasLives || (canWatchAd);
        string recoveryState = $"canPlay={hasLives} | realLives={LifeManager.Instance?.GetRealLives() ?? -1} | rewarded={AdsManager.Instance?.GetRewardedAvailabilityReason() ?? "ads_manager_missing"}";

        if (_claimLifeButton != null)
        {
            _claimLifeButton.gameObject.SetActive(claimVisible);
            _claimLifeButton.interactable = claimVisible && !_isClaimLifeFlowInProgress;
        }

        if (_closeButton != null)
        {
            _closeButton.gameObject.SetActive(true);
            _closeButton.interactable = !_isClaimLifeFlowInProgress;
        }

        _lastClaimLifeButtonVisible = claimVisible;
        _lastRecoveryState = recoveryState;
    }

    private void OnClaimLifeClicked()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[NoLivesModal] Claim life clicked | canPlay={LifeManager.Instance?.CanPlay() ?? false} | realLives={LifeManager.Instance?.GetRealLives() ?? -1} | rewarded={AdsManager.Instance?.GetRewardedAvailabilityReason() ?? "ads_manager_missing"}");
#endif

        if (LifeManager.Instance != null && LifeManager.Instance.CanPlay())
        {
            ResolveRecoveredLifeFlow();
            return;
        }

        if (!CanWatchAdForRecovery())
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[NoLivesModal] Extra life rewarded ad request rejected | reason={AdsManager.Instance?.GetRewardedAvailabilityReason() ?? "ads_manager_missing"}");
#endif
            UpdateButtons();
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[NoLivesModal] Claim button requested extra life rewarded ad.");
#endif
        _isClaimLifeFlowInProgress = true;
        UpdateButtons();
        AdsManager.Instance?.ShowRewardedAdForExtraLife();
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveAllListeners();

        if (_claimLifeButton != null)
            _claimLifeButton.onClick.RemoveAllListeners();
    }

    private void OnLivesChanged(int lives)
    {
        if (!_isVisible)
            return;

        UpdateMessage();
        UpdateButtons();

        if (LifeManager.Instance != null && LifeManager.Instance.CanPlay())
        {
            ResolveRecoveredLifeFlow();
        }
    }

    private void OnRewardedAdFlowCompleted(string context, bool rewarded)
    {
        if (!string.Equals(context, "extra_life", StringComparison.Ordinal))
            return;

        _isClaimLifeFlowInProgress = false;

        if (!_isVisible)
            return;

        if (rewarded && LifeManager.Instance != null && LifeManager.Instance.CanPlay())
        {
            ResolveRecoveredLifeFlow();
            return;
        }

        UpdateButtons();
    }

    private bool CanWatchAdForRecovery()
    {
        if (LifeManager.Instance != null && LifeManager.Instance.CanPlay())
            return false;

        return AdsManager.Instance != null && AdsManager.Instance.CanRequestRewardedAd();
    }

    protected override void RequestCloseFromOutsideClick()
    {
        OnCloseRequested();
    }

    public void HideForFlowTransition()
    {
        if (!_isVisible)
            return;

        PrepareForFlowTransitionClose();
        RequestClose();
    }

    public void PrepareForFlowTransitionClose()
    {
        _suppressAbandonOnHide = true;
    }

    private void ResolveRecoveredLifeFlow()
    {
        if (LifeManager.Instance == null || !LifeManager.Instance.CanPlay())
            return;

        _isClaimLifeFlowInProgress = false;
        PrepareForFlowTransitionClose();

        if (ShouldReturnToDefeatFlow())
        {
            UIEvents.RequestShowDefeatModal(LifeManager.Instance.GetRealLives());
            return;
        }

        RequestClose();
    }

    private bool ShouldReturnToDefeatFlow()
    {
        if (IsLevelSceneContext())
            return true;

        return false;
    }

    private void OnCloseRequested()
    {
        if (LifeManager.Instance != null && !LifeManager.Instance.CanPlay())
        {
            DismissBlockedFlow();
            return;
        }

        RequestClose();
    }

    private void DismissBlockedFlow()
    {
        _isClaimLifeFlowInProgress = false;
        PrepareForFlowTransitionClose();

        LifeManager.Instance?.NotifyLifeWallAbandoned();

        if (IsLevelSceneContext())
            UIEvents.RaiseQuitToMenuPressed();
    }

    private bool IsLevelSceneContext()
    {
        if (LevelSessionManager.Instance != null && LevelSessionManager.Instance.IsLevelActive)
            return true;

        string activeSceneName = SceneManager.GetActiveScene().name;
        return activeSceneName.StartsWith("Level_") || activeSceneName.Contains("Level");
    }

    private void RequestClose()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseModal(this);
            return;
        }

        Hide();
    }
}
