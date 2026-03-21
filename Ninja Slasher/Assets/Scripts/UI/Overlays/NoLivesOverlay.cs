using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NoLivesOverlay : UIOverlayBase
{
    [Header("No Lives UI")]
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _watchAdButton;
    [SerializeField] private Button _claimLifeButton;

    protected override void Awake()
    {
        base.Awake();
        SetupButtons();
    }

    private void OnEnable()
    {
        GameEvents.OnLivesChanged += OnLivesChanged;

        if (AdsManager.Instance != null)
            AdsManager.Instance.OnRewardedAdReadinessChanged += UpdateButtons;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEvents.OnLivesChanged -= OnLivesChanged;

        if (AdsManager.Instance != null)
            AdsManager.Instance.OnRewardedAdReadinessChanged -= UpdateButtons;
    }

    private void SetupButtons()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(Hide);

        if (_watchAdButton != null)
            _watchAdButton.onClick.AddListener(OnWatchAdClicked);

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
        Debug.Log($"[NoLivesOverlay] No lives available | realLives={LifeManager.Instance?.GetRealLives() ?? -1} | displayLives={LifeManager.Instance?.GetDisplayLives() ?? -1} | rewarded={AdsManager.Instance?.GetRewardedAvailabilityReason() ?? "ads_manager_missing"}");

        UpdateMessage();
        UpdateTimer();
        UpdateButtons();
    }

    protected override void OnHidden()
    {
        if (!LifeManager.Instance.CanPlay())
        {
            UIEvents.RaiseQuitToMenuPressed();
        }
    }

    private void UpdateMessage()
    {
        if (_messageText == null) return;

        int currentLives = LifeManager.Instance?.CurrentLives ?? 0;
        int maxLives = GameConfigManager.Config?.maxLives ?? 5;

        _messageText.text = $"No lives available\n{currentLives}/{maxLives}";
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
            _timerText.text = "Available life";
            UpdateButtons();
            return;
        }

        int minutes = timeUntilNextLife.Minutes;
        int seconds = timeUntilNextLife.Seconds;

        _timerText.text = $"Next life in: {minutes:00}:{seconds:00}";
    }

    private void UpdateButtons()
    {
        bool hasLives = LifeManager.Instance?.CanPlay() ?? false;
        bool canWatchAd = CanWatchAdForRecovery();
        bool useClaimAsRecoveryButton = _watchAdButton == null;

        if (_claimLifeButton != null)
            _claimLifeButton.gameObject.SetActive(hasLives || (useClaimAsRecoveryButton && canWatchAd));

        if (_watchAdButton != null)
            _watchAdButton.gameObject.SetActive(canWatchAd);

        if (_closeButton != null)
            _closeButton.gameObject.SetActive(true);
    }

    private void OnWatchAdClicked()
    {
        Debug.Log($"[NoLivesOverlay] Watch ad clicked | rewarded={AdsManager.Instance?.GetRewardedAvailabilityReason() ?? "ads_manager_missing"}");

        if (!CanWatchAdForRecovery())
        {
            Debug.LogWarning($"[NoLivesOverlay] Rewarded recovery request rejected | reason={AdsManager.Instance?.GetRewardedAvailabilityReason() ?? "ads_manager_missing"}");
            UpdateButtons();
            return;
        }

        Debug.Log("[NoLivesOverlay] Requesting rewarded ad for extra life from watch button.");
        AdsManager.Instance?.ShowRewardedAdForExtraLife();
    }

    private void OnClaimLifeClicked()
    {
        Debug.Log($"[NoLivesOverlay] Claim life clicked | canPlay={LifeManager.Instance?.CanPlay() ?? false} | realLives={LifeManager.Instance?.GetRealLives() ?? -1} | rewarded={AdsManager.Instance?.GetRewardedAvailabilityReason() ?? "ads_manager_missing"}");

        if (LifeManager.Instance != null && LifeManager.Instance.CanPlay())
        {
            Hide();
            UIEvents.RequestRestartLevel();
            return;
        }

        if (!CanWatchAdForRecovery())
        {
            Debug.LogWarning($"[NoLivesOverlay] Extra life rewarded ad request rejected | reason={AdsManager.Instance?.GetRewardedAvailabilityReason() ?? "ads_manager_missing"}");
            UpdateButtons();
            return;
        }

        Debug.Log("[NoLivesOverlay] Claim button requested extra life rewarded ad.");
        AdsManager.Instance?.ShowRewardedAdForExtraLife();
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveAllListeners();

        if (_watchAdButton != null)
            _watchAdButton.onClick.RemoveAllListeners();

        if (_claimLifeButton != null)
            _claimLifeButton.onClick.RemoveAllListeners();
    }

    private void OnLivesChanged(int lives)
    {
        if (!_isVisible)
            return;

        UpdateMessage();
        UpdateButtons();
    }

    private bool CanWatchAdForRecovery()
    {
        if (LifeManager.Instance != null && LifeManager.Instance.CanPlay())
            return false;

        return AdsManager.Instance != null && AdsManager.Instance.CanRequestRewardedAd();
    }
}
