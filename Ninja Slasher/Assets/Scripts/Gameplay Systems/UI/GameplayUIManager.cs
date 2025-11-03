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
    [SerializeField] private GameObject _lifeLostPanel;
    [SerializeField] private TextMeshProUGUI _puRemainingTime;

    private bool _noLivesActive = false;
    private LevelController _levelController;
    private ComboManager _comboManager;

    public void Initialize()
    {
        if (LifeManager.Instance != null)
            LifeManager.Instance.OnLivesChanged += OnLivesChanged;

        UpdateLivesUI(LifeManager.Instance?.GetDisplayLives() ?? 0);
        _lifeLostPanel.SetActive(false);

        if (_bonusTimeText != null)
            _bonusTimeText.gameObject.SetActive(false);
    }

    public void OnSceneLoaded()
    {
        UnsubscribeFromEvents();
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        _levelController = FindFirstObjectByType<LevelController>();
        if (_levelController != null)
        {
            _levelController.OnTimeChanged += OnLevelTimeChanged;
            _levelController.OnTimeExpired += OnLevelTimeExpired;
        }

        _comboManager = ComboManager.Instance;
        if (_comboManager != null)
        {
            _comboManager.OnComboUpdatedWithPosition += OnComboUpdated;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (_levelController != null)
        {
            _levelController.OnTimeChanged -= OnLevelTimeChanged;
            _levelController.OnTimeExpired -= OnLevelTimeExpired;
        }

        if (_comboManager != null)
        {
            _comboManager.OnComboUpdatedWithPosition -= OnComboUpdated;
        }
    }

    public void UpdateUI()
    {
        UpdateNoLivesTimer();
        //UpdatePowerUpsUI();
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

    //public void UpdatePowerUpsUI()
    //{
    //    var context = PowerUpManager.Instance?.context;
    //    if (context != null && context.AnyPowerUpActive())
    //    {
    //        var remaining = context.GetLowestRemainingTime();
    //        _puRemainingTime.text = $"{Mathf.CeilToInt(remaining)}s";
    //        _puRemainingTime.gameObject.SetActive(true);
    //    }
    //    else
    //    {
    //        _puRemainingTime.gameObject.SetActive(false);
    //    }
    //}

    public void ShowLifeLostPanel()
    {
        _lifeLostPanel.SetActive(true);
        StartCoroutine(HideLifeLostPanelCoroutine());
    }

    public void HideLifeLostPanel() => _lifeLostPanel.SetActive(false);

    private IEnumerator HideLifeLostPanelCoroutine()
    {
        yield return new WaitForSeconds(2f);
        HideLifeLostPanel();
    }

    public void ShowNoLivesPanel()
    {
        _noLivesActive = true;
        UIManager.Instance.ShowHideNoLivesCanvas();
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
        AudioManager.Instance.PlaySFX(SFXClip.UI_Select);
        UIManager.Instance.RestartLevel();
    }

    public void OnBackToSelectionPressed()
    {
        AudioManager.Instance.PlaySFX(SFXClip.UI_Select);
        var canvasManager = UIManager.Instance.GetComponent<CanvasManager>();
        if (canvasManager != null)
        {
            canvasManager.CloseCanvas(canvasManager.GetResultsCanvas());
        }

        LevelManager.Instance.GoToLevelSelection(confirmPendingDeduction: false);
    }

    public void ContinueToLevelSelector()
    {
        AudioManager.Instance.PlaySFX(SFXClip.UI_Select);
        var canvasManager = UIManager.Instance.GetComponent<CanvasManager>();
        if (canvasManager != null)
        {
            canvasManager.CloseCanvas(canvasManager.GetResultsCanvas());
        }

        LevelManager.Instance.GoToLevelSelection(confirmPendingDeduction: false);
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

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelFailed();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        if (LifeManager.Instance != null)
            LifeManager.Instance.OnLivesChanged -= OnLivesChanged;
    }
}