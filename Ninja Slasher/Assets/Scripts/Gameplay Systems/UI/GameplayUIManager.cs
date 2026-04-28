using System;
using TMPro;
using UnityEngine;

public class GameplayUIManager : MonoBehaviour
{
    [Header("GAMEPLAY UI")]
    [SerializeField] private TextMeshProUGUI _livesAmount;
    [SerializeField] private TextMeshProUGUI _livesTimerText;
    [SerializeField] private GameObject _livesTimerObj;
    [SerializeField] private TextMeshProUGUI _noLivesTimerText;
    [SerializeField] private TextMeshProUGUI _levelTimerText;
    [SerializeField] private TextMeshProUGUI _bonusTimeText;
    [SerializeField] private UIPunchScaleFeedback _bonusTimeFeedback;

    private UIAudioContext _audioContext;
    private bool _noLivesActive = false;

    private void Awake()
    {
        _audioContext = GetComponentInParent<UIAudioContext>();
        ResolveBonusTimeFeedback();
        HideBonusTimeFeedbackImmediate();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        HideBonusTimeFeedbackImmediate();
    }

    public void Initialize()
    {
        UpdateLivesUI(LifeManager.Instance?.GetDisplayLives() ?? 0);
        HideBonusTimeFeedbackImmediate();
    }

    public void OnSceneLoaded()
    {
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        UnsubscribeFromEvents();

        GameEvents.OnLivesChanged += OnLivesChanged;
        GameEvents.OnLevelTimeChanged += OnLevelTimeChanged;
        GameEvents.OnLevelTimeExpired += OnLevelTimeExpired;
        GameEvents.OnLevelTimeBonus += ShowBonusTimeText;
        UIEvents.OnUILivesUpdateRequested += UpdateLivesUI;
    }

    private void UnsubscribeFromEvents()
    {
        GameEvents.OnLivesChanged -= OnLivesChanged;
        GameEvents.OnLevelTimeChanged -= OnLevelTimeChanged;
        GameEvents.OnLevelTimeExpired -= OnLevelTimeExpired;
        GameEvents.OnLevelTimeBonus -= ShowBonusTimeText;
        UIEvents.OnUILivesUpdateRequested -= UpdateLivesUI;
    }

    public void UpdateUI()
    {
        UpdateNoLivesTimer();
    }

    private void UpdateNoLivesTimer()
    {
        var lm = LifeManager.Instance;
        if (lm == null || !lm.IsInitialized) return;

        bool needsTimer = lm.GetRealLives() < 3;

        if (_livesTimerObj != null)
            _livesTimerObj.SetActive(needsTimer);

        if (!needsTimer) return;

        TimeSpan time = lm.GetTimeToNextLife();
        string formatted = $"{time.Minutes:D2}:{time.Seconds:D2}";

        if (_noLivesTimerText != null)
            _noLivesTimerText.text = formatted;

        if (_livesTimerText != null)
            _livesTimerText.text = formatted;
    }

    public void ShowNoLivesPanel()
    {
        _noLivesActive = true;
        UIEvents.RequestShowNoLivesModal();
    }

    public void UpdateLivesUI(int lives)
    {
        if (_livesAmount != null)
            _livesAmount.text = lives.ToString();
    }

    public void OnRetryPressed()
    {
        UIEvents.RaiseRetryPressed();
    }

    public void OnBackToSelectionPressed()
    {
        UIEvents.RequestShowLifeLostPanel();
        UIEvents.RequestHideVictoryModal();
        UIEvents.RaiseQuitToMenuPressed();
    }

    public void ContinueToLevelSelector()
    {
        UIEvents.RequestHideVictoryModal();
        UIEvents.RaiseQuitToMenuPressed();
    }

    private void OnLivesChanged(int lives)
    {
        UpdateLivesUI(lives);

        if (_noLivesActive && LifeManager.Instance.GetRealLives() > 0)
        {
            _noLivesActive = false;
            UIEvents.RequestHideNoLivesModal();
        }
    }

    private void ShowBonusTimeText(float bonus)
    {
        if (_bonusTimeText == null)
            return;

        _bonusTimeText.text = $"+{bonus:F0}s";
        ResolveBonusTimeFeedback();

        if (_bonusTimeFeedback != null)
        {
            _bonusTimeFeedback.Play();
            return;
        }

        _bonusTimeText.gameObject.SetActive(true);
    }

    private void ResolveBonusTimeFeedback()
    {
        if (_bonusTimeFeedback == null && _bonusTimeText != null)
            _bonusTimeFeedback = _bonusTimeText.GetComponent<UIPunchScaleFeedback>();
    }

    private void HideBonusTimeFeedbackImmediate()
    {
        if (_bonusTimeFeedback != null)
        {
            _bonusTimeFeedback.HideImmediate();
            return;
        }

        if (_bonusTimeText != null)
            _bonusTimeText.gameObject.SetActive(false);
    }

    private void OnLevelTimeChanged(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        _levelTimerText.text = $"{minutes:D2}:{seconds:D2}";
    }

    private void OnLevelTimeExpired()
    {
        if (_levelTimerText != null)
            _levelTimerText.text = "00:00";
    }
}
