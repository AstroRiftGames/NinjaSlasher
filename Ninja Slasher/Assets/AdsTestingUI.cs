using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdsTestingUI : MonoBehaviour
{
    [Header("Botones")]
    [SerializeField] private Button rewardedButton;
    [SerializeField] private Button interstitialButton;

    [Header("Info")]
    [SerializeField] private TextMeshProUGUI statusText;

    void Start()
    {
        SetupButtons();

        InvokeRepeating(nameof(UpdateUI), 2f, 1f);
    }

    void SetupButtons()
    {
        if (rewardedButton != null)
            rewardedButton.onClick.AddListener(() => ShowRewardedAd());

        if (interstitialButton != null)
            interstitialButton.onClick.AddListener(() => ShowInterstitialAd());
    }

    void ShowRewardedAd()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.ShowRewardedAd();
            UpdateStatus("Showing Rewarded Ad...");
        }
        else
        {
            UpdateStatus("AdsManager not found!");
        }
    }

    void ShowInterstitialAd()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.ShowInterstitialAd();
            UpdateStatus("Showing Interstitial Ad...");
        }
        else
        {
            UpdateStatus("AdsManager not found!");
        }
    }

    void UpdateUI()
    {
        if (AdsManager.Instance == null) return;

        string status = "";
        status += $"Rewarded: {(AdsManager.Instance.IsRewardedAdReady() ? "Ready" : "Loading...")}\n";
        status += $"Interstitial: {(AdsManager.Instance.IsInterstitialAdReady() ? "Ready" : "Loading...")}\n";

        if (statusText != null)
        {
            statusText.text = status;
        }

        if (rewardedButton != null)
        {
            rewardedButton.interactable = AdsManager.Instance.IsRewardedAdReady();
        }

        if (interstitialButton != null)
        {
            interstitialButton.interactable = AdsManager.Instance.IsInterstitialAdReady();
        }
    }

    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        Debug.Log(message);

        Invoke(nameof(ClearStatusMessage), 3f);
    }

    void ClearStatusMessage()
    {
        UpdateUI();
    }

    public void OnGameOver()
    {
        ShowInterstitialAd();
    }

    public void OnNeedExtraLife()
    {
        ShowRewardedAd();
    }
}