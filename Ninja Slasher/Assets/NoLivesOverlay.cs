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

    private float _nextLifeTime;

    protected override void Awake()
    {
        base.Awake();
        SetupButtons();
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
        Debug.Log("[NoLivesOverlay] Sin vidas disponibles");

        UpdateMessage();
        UpdateTimer();
        UpdateButtons();
    }

    protected override void OnHidden()
    {
        Debug.Log("[NoLivesOverlay] Overlay ocultado");
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
            _timerText.text = "¡Vida disponible!";
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

        if (_claimLifeButton != null)
            _claimLifeButton.interactable = hasLives;
    }

    private void OnWatchAdClicked()
    {
        Debug.Log("[NoLivesOverlay] Ver anuncio para vida extra");
        // AdsManager.Instance?.ShowRewardedAdForExtraLife();
    }

    private void OnClaimLifeClicked()
    {
        if (LifeManager.Instance != null && LifeManager.Instance.CanPlay())
        {
            Hide();
        }
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
}