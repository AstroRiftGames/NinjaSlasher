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
        GameEvents.OnLivesChanged -= OnLivesChanged;
        UIEvents.OnUILivesUpdateRequested -= UpdateLivesUI;
    }

    public void Initialize()
    {
        GameEvents.OnLivesChanged += OnLivesChanged;

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
        //UnsubscribeFromEvents();
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        GameEvents.OnLivesChanged += OnLivesChanged;
        GameEvents.OnLevelTimeChanged += OnLevelTimeChanged;
        GameEvents.OnLevelTimeExpired += OnLevelTimeExpired;
        GameEvents.OnComboUpdated += OnComboUpdated;
        UIEvents.OnUILivesUpdateRequested += UpdateLivesUI;
    }

    public void UpdateUI()
    {
        UpdateNoLivesTimer();
    }

    private void UpdateNoLivesTimer()
    {
        if (LifeManager.Instance.GetRealLives() < 3)
        {
            var time = LifeManager.Instance.GetTimeToNextLife();
            _noLivesTimerText.text = $"{time.Minutes:D2}:{time.Seconds:D2}";
            _livesTimerText.text = $"{time.Minutes:D2}:{time.Seconds:D2}";

            _livesTimerObj.SetActive(true);
        }

        if (LifeManager.Instance.GetRealLives() >= 3)
        {
            _livesTimerObj.SetActive(false);
        }
    }

    private void UpdatePowerUpsUI()
    {
        if (_puRemainingTime == null || PowerUpManager.Instance == null)
        {
            if (_puRemainingTime != null)
                _puRemainingTime.text = "";
            return;
        }

        /*
        var context = PowerUpManager.Instance.context;
        if (context == null)
        {
            _puRemainingTime.text = "";
            return;
        }

        bool isPowerUpActive = context.AnyPowerUpActive();

        if (isPowerUpActive)
        {
            var remaining = context.GetLowestRemainingTime();
            int seconds = Mathf.CeilToInt(remaining);
            if (seconds > 0)
            {
                _puRemainingTime.text = $"{seconds}s";
            }
            else
            {
                _puRemainingTime.text = "";
            }
        }
        else
        {
            _puRemainingTime.text = "";
        }
        */
    }

    public void ShowNoLivesPanel()
    {
        _noLivesActive = true;
        //UIManager.Instance.ShowNoLivesOverlay();
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
        //AudioManager.Instance.PlaySFX(SFXClip.UI_Select);
        AudioService.Instance?.PlaySFX(_audioContext.Audio.select);

        UIManager.Instance.RestartLevel();
        UIManager.Instance.ShowHideLifeLostCanvas();
    }

    public void OnBackToSelectionPressed()
    {
        UIManager.Instance.ShowHideLifeLostCanvas();

        //AudioManager.Instance.PlaySFX(SFXClip.UI_Select);

        AudioService.Instance?.PlaySFX(_audioContext.Audio.select);

        //UIManager.Instance.HideResultsModal();
        UIEvents.RequestHideResultsModal();
        GameManager.Instance.GoToLevelSelection(confirmPendingDeduction: false);
    }

    public void ContinueToLevelSelector()
    {
        //AudioManager.Instance.PlaySFX(SFXClip.UI_Select);
        AudioService.Instance?.PlaySFX(_audioContext.Audio.select);
        //UIManager.Instance.HideResultsModal();
        UIEvents.RequestHideResultsModal();
        GameManager.Instance.GoToLevelSelection(confirmPendingDeduction: false);
    }

    private void OnLivesChanged(int lives)
    {
        UpdateLivesUI(lives);

        if (_noLivesActive && LifeManager.Instance.GetRealLives() > 0)
        {
            _noLivesActive = false;
            UIManager.Instance.ShowHideNoLivesCanvas();
        }
    }

    private void OnComboUpdated(int comboLevel, Vector3 position)
    {
        if (comboLevel < 2) return;

        float bonus = comboLevel switch
        {
            2 => 3f,
            3 => 4f,
            4 => 6f,
            _ => 3f
        };

        ShowBonusTimeText(bonus);
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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelFailed();
        }
    }
}
