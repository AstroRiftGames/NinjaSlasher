using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NoLivesModal : UIModalBase
{
    public enum FlowContext
    {
        LevelLifeWall,
        PreGameRecovery
    }

    [Header("No Lives UI")]
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _claimLifeButton;

    private FlowContext _flowContext = FlowContext.LevelLifeWall;
    private bool _suppressAbandonOnHide;
    private bool _isClaimLifeFlowInProgress;
    private bool _isResolvingRecoveredLifeFlow;
    private bool _suppressLifeDisplayRefresh;

    public void SetFlowContext(FlowContext context)
    {
        _flowContext = context;
    }

    protected override void Awake()
    {
        base.Awake();
        SetupButtons();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
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
        PauseController.Instance?.RequestPause(PauseSource.NoLives);
        ResetRecoveredLifeFlowFlags();
        UpdateMessage();
        UpdateTimer();
        UpdateButtons();
    }

    protected override void OnHidden()
    {
        PauseController.Instance?.ReleasePause(PauseSource.NoLives);

        if (_suppressAbandonOnHide)
        {
            _suppressAbandonOnHide = false;
            ResetRecoveredLifeFlowFlags();
            return;
        }

        if (_isClaimLifeFlowInProgress || (AdsManager.Instance != null && AdsManager.Instance.IsRewardedAdFlowInProgress("extra_life")))
            return;

        if (_flowContext == FlowContext.LevelLifeWall && LifeManager.Instance.RequiresLifeRecoveryForCurrentAttempt())
        {
            LifeManager.Instance?.NotifyLifeWallAbandoned();
            UIEvents.RaiseQuitToMenuPressed();
        }

        ResetRecoveredLifeFlowFlags();
    }

    private bool ShouldSuppressLifeDisplayRefresh()
    {
        return _suppressLifeDisplayRefresh || _isResolvingRecoveredLifeFlow || _suppressAbandonOnHide;
    }

    private void BeginRecoveredLifeFlowClose()
    {
        _isResolvingRecoveredLifeFlow = true;
        _suppressLifeDisplayRefresh = true;
    }

    private void ResetRecoveredLifeFlowFlags()
    {
        _isResolvingRecoveredLifeFlow = false;
        _suppressLifeDisplayRefresh = false;
    }

    private void UpdateMessage()
    {
        if (_messageText == null) return;

        int effectiveLives = LifeManager.Instance?.GetEffectiveLivesForCurrentAttempt() ?? 0;
        int maxLives = LifeManager.Instance?.GetMaxLives() ?? GameConfigManager.Config?.maxLives ?? 5;

        bool suppressRefresh = ShouldSuppressLifeDisplayRefresh();
        if (!suppressRefresh)
            _messageText.text = $"Sin vidas disponibles\n{effectiveLives}/{maxLives}";
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
        bool canContinueAttempt = LifeManager.Instance?.CanContinueCurrentAttempt() ?? false;
        bool canWatchAd = CanWatchAdForRecovery();
        bool claimVisible = canContinueAttempt || canWatchAd;

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

    }

    private void OnClaimLifeClicked()
    {
        bool canPlayAfterPendingDeduction = LifeManager.Instance?.CanContinueCurrentAttempt() ?? false;
        bool canWatchAd = CanWatchAdForRecovery();

        if (LifeManager.Instance != null && canPlayAfterPendingDeduction)
        {
            ResolveRecoveredLifeFlow();
            return;
        }

        if (!canWatchAd)
        {
            UpdateButtons();
            return;
        }

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

        bool canContinueCurrentAttempt = LifeManager.Instance != null && LifeManager.Instance.CanContinueCurrentAttempt();
        if ((_isClaimLifeFlowInProgress && canContinueCurrentAttempt) || ShouldSuppressLifeDisplayRefresh())
        {
            if (canContinueCurrentAttempt)
                BeginRecoveredLifeFlowClose();

            if (canContinueCurrentAttempt)
                ResolveRecoveredLifeFlow();

            return;
        }

        UpdateMessage();
        UpdateButtons();

        if (canContinueCurrentAttempt)
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

        if (rewarded && LifeManager.Instance != null && LifeManager.Instance.CanContinueCurrentAttempt())
        {
            BeginRecoveredLifeFlowClose();
            ResolveRecoveredLifeFlow();
            return;
        }

        UpdateButtons();
    }

    private bool CanWatchAdForRecovery()
    {
        if (LifeManager.Instance != null && !LifeManager.Instance.RequiresLifeRecoveryForCurrentAttempt())
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
        PrepareForFlowTransitionClose();

        if (LifeManager.Instance == null || !LifeManager.Instance.CanContinueCurrentAttempt())
            return;

        BeginRecoveredLifeFlowClose();
        _isClaimLifeFlowInProgress = false;

        if (ShouldReturnToDefeatFlow())
        {
            UIEvents.RequestShowDefeatModal(LifeManager.Instance.GetEffectiveLivesForCurrentAttempt());
            RequestClose();
            return;
        }

        RequestClose();
    }

    private bool ShouldReturnToDefeatFlow()
    {
        return _flowContext == FlowContext.LevelLifeWall;
    }

    private void OnCloseRequested()
    {
        if (_isClaimLifeFlowInProgress && LifeManager.Instance != null && LifeManager.Instance.RequiresLifeRecoveryForCurrentAttempt())
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

        if (_flowContext == FlowContext.LevelLifeWall)
        {
            LifeManager.Instance?.NotifyLifeWallAbandoned();
            UIEvents.RaiseQuitToMenuPressed();
            return;
        }

        RequestClose();
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
