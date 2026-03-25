using System;
using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class GameplayUIManager : MonoBehaviour
{
    [Header("GAMEPLAY UI")]
    [SerializeField] private TextMeshProUGUI _livesAmount;
    [SerializeField] private TextMeshProUGUI _livesTimerText;
    [SerializeField] private GameObject _livesTimerObj;
    [SerializeField] private TextMeshProUGUI _noLivesTimerText;
    [SerializeField] private TextMeshProUGUI _levelTimerText;
    [SerializeField] private TextMeshProUGUI _bonusTimeText;
    [SerializeField] private TextMeshProUGUI _puRemainingTime;

    private UIAudioContext _audioContext;

    private bool _noLivesActive = false;

    private void Awake()
    {
        _audioContext = GetComponentInParent<UIAudioContext>();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    public void Initialize()
    {
        UpdateLivesUI(LifeManager.Instance?.GetDisplayLives() ?? 0);

        if (_bonusTimeText != null)
            _bonusTimeText.gameObject.SetActive(false);

        if (_puRemainingTime != null)
        {
            _puRemainingTime.text = "";
            _puRemainingTime.gameObject.SetActive(true);
        }

        UpdatePowerUpsUI();
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

    private void UpdatePowerUpsUI()
    {
        if (_puRemainingTime == null || PowerUpManager.Instance == null)
        {
            if (_puRemainingTime != null)
                _puRemainingTime.text = "";
            return;
        }
    }

    public void ShowNoLivesPanel()
    {
        _noLivesActive = true;
        UIEvents.RequestShowNoLivesOverlay();
    }

    public void UpdateLivesUI(int lives)
    {
        if (_livesAmount != null)
        {
            _livesAmount.text = lives.ToString();
        }
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
            UIEvents.RequestHideNoLivesOverlay();
        }
    }

    private void ShowBonusTimeText(float bonus)
    {
        if (_bonusTimeText == null) return;

        StopAllCoroutines();

        _bonusTimeText.text = $"+{bonus:F0}s";
        _bonusTimeText.gameObject.SetActive(true);

        StartCoroutine(AnimateBonusText());
    }

    private IEnumerator AnimateBonusText()
    {
        if (_bonusTimeText == null) yield break;

        var rectTransform = _bonusTimeText.GetComponent<RectTransform>();
        var canvasGroup = _bonusTimeText.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = _bonusTimeText.gameObject.AddComponent<CanvasGroup>();
        }

        Vector3 originalScale = rectTransform.localScale;
        canvasGroup.alpha = 1f;

        rectTransform.localScale = originalScale * 0.5f;
        rectTransform.DOScale(originalScale * 1.3f, 0.2f).SetEase(DG.Tweening.Ease.OutBack);

        yield return new WaitForSeconds(0.2f);

        rectTransform.DOScale(originalScale, 0.15f).SetEase(DG.Tweening.Ease.InOutQuad);

        yield return new WaitForSeconds(1.2f);

        canvasGroup.DOFade(0f, 0.3f).SetEase(DG.Tweening.Ease.InQuad)
            .OnComplete(() => _bonusTimeText.gameObject.SetActive(false));
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
