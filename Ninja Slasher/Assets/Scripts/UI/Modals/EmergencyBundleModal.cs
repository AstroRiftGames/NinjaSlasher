using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmergencyBundleModal : UIModalBase
{
    [Header("Bundle Info")]
    [SerializeField] private Image _bundleIcon;
    [SerializeField] private TMP_Text _bundleNameText;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private TMP_Text _rewardText;

    [Header("Countdown")]
    [SerializeField] private TMP_Text _countdownText;

    [Header("Buttons")]
    [SerializeField] private Button _buyBtn;
    [SerializeField] private Button _dismissBtn;

    private EmergencyBundleOffer _currentOffer;
    private Coroutine _countdownCoroutine;

    #region ENABLE / DISABLE

    private void OnEnable()
    {
        _buyBtn?.onClick.AddListener(OnBuyClicked);
        _dismissBtn?.onClick.AddListener(OnDismissClicked);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _buyBtn?.onClick.RemoveListener(OnBuyClicked);
        _dismissBtn?.onClick.RemoveListener(OnDismissClicked);
    }

    #endregion

    #region SHOW / HIDE

    public void ShowWithOffer(EmergencyBundleOffer offer)
    {
        _currentOffer = offer;
        PopulateUI(offer);
    }

    protected override void OnShown()
    {
        base.OnShown();
        if (_currentOffer != null)
            _countdownCoroutine = StartCoroutine(CountdownRoutine(_currentOffer.OfferDurationSeconds));
    }

    protected override void OnHidden()
    {
        base.OnHidden();
        StopCountdown();
        _currentOffer = null;

        EmergencyBundleService.Instance?.OnOfferDismissed();
    }

    #endregion

    #region UI POPULATION

    private void PopulateUI(EmergencyBundleOffer offer)
    {
        var display = offer.Product?.display;
        var reward  = offer.Product?.bundleReward;

        if (_bundleIcon != null && display?.icon != null)
            _bundleIcon.sprite = display.icon;

        if (_bundleNameText != null)
            _bundleNameText.text = display?.bundleName ?? string.Empty;

        if (_priceText != null)
            _priceText.text = offer.LocalizedPrice;

        if (_rewardText != null)
            _rewardText.text = BuildRewardDescription(reward);
    }

    private string BuildRewardDescription(BundleRewardData reward)
    {
        if (reward == null) return string.Empty;

        var sb = new System.Text.StringBuilder();

        // Lives
        if (reward.unlimitedLives && reward.unlimitedLivesDurationMinutes > 0f)
            sb.Append($"Vidas ilimitadas {reward.unlimitedLivesDurationMinutes:0} min");
        else if (reward.regularLivesCount > 0)
            sb.Append($"+{reward.regularLivesCount} {(reward.regularLivesCount == 1 ? "vida" : "vidas")}");

        // Power-ups
        if (reward.powerUps != null)
        {
            foreach (var entry in reward.powerUps)
            {
                if (entry.quantity <= 0) continue;
                if (sb.Length > 0) sb.Append(" - ");
                sb.Append($"{GetPowerUpDisplayName(entry.type)} x{entry.quantity}");
            }
        }

        // Coins
        if (reward.coins > 0)
        {
            if (sb.Length > 0) sb.Append(" - ");
            sb.Append($"{reward.coins} monedas");
        }

        return sb.ToString();
    }

    private static string GetPowerUpDisplayName(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.ExtraTime:
                return "Tiempo Extra";
            case PowerUpType.DashTurbo:
                return "Turbo de Dash";
            case PowerUpType.ParryPerfect:
                return "Parry Perfecto";
            case PowerUpType.ComboMaster:
                return "Maestro del Combo";
            case PowerUpType.SecondChance:
                return "Segunda Oportunidad";
            case PowerUpType.HawkVision:
                return "Ojo de Halcon";
            case PowerUpType.EnhancedParry:
                return "Parry Potenciado";
            default:
                return type.ToString();
        }
    }

    #endregion

    #region COUNTDOWN

    private IEnumerator CountdownRoutine(float durationSeconds)
    {
        float remaining = durationSeconds;

        while (remaining > 0f)
        {
            if (_countdownText != null)
            {
                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = Mathf.FloorToInt(remaining % 60f);
                _countdownText.text = $"{minutes:D2}:{seconds:D2}";
            }

            yield return null;
            remaining -= Time.unscaledDeltaTime;
        }

        if (_countdownText != null) _countdownText.text = "00:00";
        OnCountdownExpired();
    }

    private void OnCountdownExpired()
    {
        if (EmergencyBundleService.Instance != null)
            EmergencyBundleService.Instance.OnOfferExpired();
        else
            UIEvents.RequestHideEmergencyBundleModal();
    }

    private void StopCountdown()
    {
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }
    }

    #endregion

    #region BUTTON HANDLERS

    private void OnBuyClicked()
    {
        if (_currentOffer?.Product == null) return;

        string productId = _currentOffer.Product.PrimaryProductId;
        if (string.IsNullOrEmpty(productId)) return;

        AnalyticsManager.Instance?.RecordPurchaseStarted(
            productId,
            AnalyticsManager.ProductCategoryStr(_currentOffer.Product.category),
            "paywall"
        );

        if (IAPManager.Instance != null)
            IAPManager.Instance.PurchaseProduct(productId);
        else
            Debug.LogWarning("[EmergencyBundleModal] IAPManager no disponible.");
    }

    private void OnDismissClicked() => DismissOffer();

    private void DismissOffer()
    {
        if (EmergencyBundleService.Instance != null)
            EmergencyBundleService.Instance.OnOfferDismissed();
        else
            UIEvents.RequestHideEmergencyBundleModal();
    }

    #endregion
}
