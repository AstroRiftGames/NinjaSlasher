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
    [SerializeField] private TextMeshProUGUI _comboCountText;
    [SerializeField] private TextMeshProUGUI _levelTimerText;
    [SerializeField] private TextMeshProUGUI _bonusTimeText;
    [SerializeField] private GameObject _lifeLostPanel;
    [SerializeField] private TextMeshProUGUI _powerUpsText;

    private bool _noLivesActive = false;
    private LevelController _levelController;
    private ComboManager _comboManager;

    public void Initialize()
    {
        if (LifeManager.Instance != null)
            LifeManager.Instance.OnLivesChanged += OnLivesChanged;

        UpdateLivesUI(LifeManager.Instance?.GetDisplayLives() ?? 0);
        _comboCountText.gameObject.SetActive(false);
        _bonusTimeText.gameObject.SetActive(false);
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

        _comboManager = ComboManager.Instance;
        if (_comboManager != null)
        {
            _comboManager.OnComboUpdated += OnComboUpdated;
            _comboManager.OnComboEnded += OnComboEnded;
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
            _comboManager.OnComboUpdated -= OnComboUpdated;
            _comboManager.OnComboEnded -= OnComboEnded;
        }
    }

    public void UpdateUI()
    {
        UpdateNoLivesTimer();
        UpdatePowerUpsUI();
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

    public void UpdatePowerUpsUI()
    {
        var context = PowerUpManager.Instance?.context;
        if (_powerUpsText == null || context == null)
            return;

        string status = "";
        if (context.ExtraTimeActive) status += "Power up activo: Tiempo Extra\n";
        if (context.DashTurboActive) status += "Power up activo: Dash Turbo\n";
        if (context.ParryPerfectActive) status += "Power up activo: Parry Perfect\n";
        if (context.ComboMasterActive) status += "Power up activo: Combo Master\n";
        if (context.SecondChanceActive) status += "Power up activo: Second Chance\n";

        _powerUpsText.text = status.Length > 0 ? status : "Sin Power Ups activos";
    }

    public void ShowLifeLostPanel() => _lifeLostPanel.SetActive(true);
    public void HideLifeLostPanel() => _lifeLostPanel.SetActive(false);

    public void ShowNoLivesPanel()
    {
        UIManager.Instance.ShowHideNoLivesCanvas();
        _noLivesActive = true;
    }

    public void UpdateLivesUI(int lives)
    {
        if (_livesAmount != null)
            _livesAmount.text = $"{lives}";
    }

    public void OnRetryPressed()
    {
        HideLifeLostPanel();

        GameManager.Instance.RestartLevel();
    }

    public void OnBackToSelectionPressed()
    {
        HideLifeLostPanel();
        GameManager.Instance.GoToLevelSelection();
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

    private void OnComboUpdated(int comboLevel)
    {
        _comboCountText.text = $"Combo: {comboLevel}";
        _comboCountText.gameObject.SetActive(true);

        if (comboLevel >= 2)
        {
            float bonus = comboLevel switch
            {
                2 => 3f,
                3 => 4f,
                4 => 6f,
                _ => 3f
            };
            _bonusTimeText.text = $"+{bonus:F0}s";
            _bonusTimeText.gameObject.SetActive(true);
            StartCoroutine(HideBonusCoroutine());
        }
    }

    private void OnComboEnded() => _comboCountText.gameObject.SetActive(false);

    private IEnumerator HideBonusCoroutine()
    {
        yield return new WaitForSeconds(2f);
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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelFailed();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        if (LifeManager.Instance != null)
            LifeManager.Instance.OnLivesChanged -= OnLivesChanged;
    }
}