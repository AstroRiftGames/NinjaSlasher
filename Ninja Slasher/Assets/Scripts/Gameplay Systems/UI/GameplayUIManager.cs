using System.Collections;
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
    }

    private void UnsubscribeFromEvents()
    {
        if (_levelController != null)
        {
            _levelController.OnTimeChanged -= OnLevelTimeChanged;
            _levelController.OnTimeExpired -= OnLevelTimeExpired;
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