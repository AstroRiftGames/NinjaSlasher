using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviourSingleton<UIManager>
{
    [Header("SCREEN TRANSITION")]
    [SerializeField] private Animator _transitionAnim;
    [SerializeField] private float _transitionTime;

    [Header("CANVAS")]
    [SerializeField] private Canvas _splashCanvas;
    [SerializeField] private Canvas _levelsCanvas;
    [SerializeField] private Canvas _preGameCanvas;
    [SerializeField] private Canvas _gameplayCanvas;
    [SerializeField] private Canvas _creditsCanvas;
    [SerializeField] private Canvas _pauseCanvas;
    [SerializeField] private Canvas _configCanvas;

    [Header("LEVEL SELECTOR BUTTONS")]
    [SerializeField] private Button[] levelButtons;
    [SerializeField] private Button _testLevelButton;
    [SerializeField] private Button _configButton;
    [SerializeField] private Button _musicButton;
    [SerializeField] private Button _sfxButton;
    [SerializeField] private Button _creditsButton;
    [SerializeField] private Animator _configPanelAnim;
    [SerializeField] bool _isOpen = false;

    [Header("DAILY REWARDS")]
    [SerializeField] private DailyRewardUI _dailyRewardUI;

    [Header("PREGAME BUTTONS")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _playButton;
    [SerializeField] private string _pendingSceneName;

    [SerializeField] private Transform _powerUpsPanel;
    [SerializeField] private PowerUpSlotUI _powerUpSlotPrefab;
    private List<PowerUpSlotUI> _slots = new List<PowerUpSlotUI>();

    [Header("GAMEPLAY BUTTONS")]
    [SerializeField] private Button _pauseButton;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _quitButton;

    [SerializeField] private string[] sceneNames;
    [SerializeField] private TextMeshProUGUI _livesText;
    [SerializeField] private GameObject _noLivesPanel;
    [SerializeField] private TextMeshProUGUI _noLivesTimerText;
    [SerializeField] private TextMeshProUGUI _comboCountText;
    [SerializeField] private TextMeshProUGUI _levelTimerText;
    [SerializeField] private TextMeshProUGUI _bonusTimeText;

    [Header("LIFE LOST PANEL")]
    [SerializeField] private GameObject _lifeLostPanel;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _backToSelectionButton;

    private bool _noLivesActive = false;
    private LevelController _levelController;
    private ComboManager _comboManager;
    [SerializeField] private PowerUpBase[] allPowerUpBases;

    [Header("DEBUG")]
    [SerializeField] private TextMeshProUGUI debugStarsText;
    [SerializeField] private TextMeshProUGUI _powerUpsText;
#if UNITY_EDITOR
    [SerializeField] private Button deleteSaveButton;
#endif

    AudioToggle _audioToggle;
    private void Start()
    {
        _dailyRewardUI = GetComponentInChildren<DailyRewardUI>();
        SetButtons();
        _audioToggle = GetComponent<AudioToggle>();
        if (LifeManager.Instance != null)
            LifeManager.Instance.OnLivesChanged += OnLivesChanged;

        UpdateLivesUI(LifeManager.Instance.CurrentLives);
        _comboCountText.gameObject.SetActive(false);
        _bonusTimeText.gameObject.SetActive(false);

        _lifeLostPanel.SetActive(false);        
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

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

        if (LifeManager.Instance != null)
            LifeManager.Instance.OnLivesChanged -= OnLivesChanged;
    }

    private void Update()
    {
        if (_noLivesPanel.activeSelf)
        {
            var time = LifeManager.Instance.GetTimeToNextLife();
            _noLivesTimerText.text = $"Next life in: {time.Minutes:D2}:{time.Seconds:D2}";
        }

        UpdatePowerUpsUI();

        if (Input.GetKeyDown(KeyCode.F))
        {
            ShowHidePauseCanvas();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_levelController != null)
        {
            _levelController.OnTimeChanged -= OnLevelTimeChanged;
            _levelController.OnTimeExpired -= OnLevelTimeExpired;
        }

        _levelController = FindFirstObjectByType<LevelController>();
        if (_levelController != null)
        {
            _levelController.OnTimeChanged += OnLevelTimeChanged;
            _levelController.OnTimeExpired += OnLevelTimeExpired;
        }

        if (_comboManager != null)
        {
            _comboManager.OnComboUpdated -= OnComboUpdated;
            _comboManager.OnComboEnded -= OnComboEnded;
        }

        _comboManager = ComboManager.Instance;
        if (_comboManager != null)
        {
            _comboManager.OnComboUpdated += OnComboUpdated;
            _comboManager.OnComboEnded += OnComboEnded;
        }
    }

    private void SetButtons()
    {
        //Levels Scene
        _configButton.onClick.AddListener(OpenCloseConfigPanel);
        _musicButton.onClick.AddListener(MusicOnOff);
        _sfxButton.onClick.AddListener(SFXOnOff);
        _creditsButton.onClick.AddListener(ShowHideCreditsCanvas);
        _testLevelButton.onClick.AddListener(LoadDebugTestScene);

        //Pre-Game
        _closeButton.onClick.AddListener(ShowHidePreGameCanvas);
        _playButton.onClick.AddListener(ShowHidePreGameCanvas);
        
        //Gameplay
        _pauseButton.onClick.AddListener(ShowHidePauseCanvas);
        _resumeButton.onClick.AddListener(ShowHidePauseCanvas);
        _restartButton.onClick.AddListener(RestartLevel);
        _quitButton.onClick.AddListener(ShowLevelSelector);
        _retryButton.onClick.AddListener(OnRetryPressed);
        _backToSelectionButton.onClick.AddListener(OnBackToSelectionPressed);
        
        for (int i = 0; i < levelButtons.Length; i++)
        {
            string sceneName = sceneNames[i];
            levelButtons[i].onClick.AddListener(() => ShowConfirmationPanel(sceneName));
        }

#if UNITY_EDITOR
        if (deleteSaveButton != null)
            deleteSaveButton.onClick.AddListener(DeleteSaveDataFromUI);
#endif
    }
    
    public void ShowConfirmationPanel(string sceneName)
    {
        _pendingSceneName = sceneName;
        ShowHidePreGameCanvas();

        _playButton.onClick.RemoveAllListeners();
        _playButton.onClick.AddListener(OnConfirmLevelSelection);

        _closeButton.onClick.RemoveAllListeners();
        _closeButton.onClick.AddListener(CancelLevelSelection);
    }

    private void OnConfirmLevelSelection()
    {
        ShowHidePreGameCanvas();
        LoadLevelScene(_pendingSceneName);
    }

    private void CancelLevelSelection()
    {
        ShowHidePreGameCanvas();
        _pendingSceneName = null;
    }
    public void LoadLevelScene(string sceneName)
    {
        StartCoroutine(LoadLevelSceneCo(sceneName));
    }

    private IEnumerator LoadLevelSceneCo(string sceneName)
    {
        _transitionAnim.SetTrigger("Start");
        yield return new WaitForSeconds(_transitionTime);
        _levelsCanvas.enabled = false;
        SceneManager.LoadScene(sceneName);
        _transitionAnim.SetTrigger("End");
        _gameplayCanvas.enabled = true;
    }

    public void RestartLevel()
    {
        if (!LifeManager.Instance.CanPlay())
        {
            ShowNoLivesPanel();
            return;
        }
        string sceneName = SceneManager.GetActiveScene().name;
        LoadLevelScene(sceneName);
        ShowHidePauseCanvas();
    }

    public void ShowLevelSelector()
    {
        StartCoroutine(ShowLevelSelectorCo());
        UpdateLivesUI(LifeManager.Instance.CurrentLives);
        ShowStarsDebug();

        CheckAndShowDailyRewards();
    }

    IEnumerator ShowLevelSelectorCo()
    {
        _transitionAnim.SetTrigger("OpeningStart");
        yield return new WaitForSeconds(_transitionTime);
        _splashCanvas.enabled = false;
        _levelsCanvas.enabled = true;
        _pauseCanvas.enabled = false;
        if (!LifeManager.Instance.CanPlay())
            ShowNoLivesPanel();

        _transitionAnim.SetTrigger("End");
    }

    void CheckAndShowDailyRewards()
    {
        Debug.Log("[UIManager] CheckAndShowDailyRewards() llamado");

        if (DailyRewardSystem.Instance == null)
        {
            Debug.LogError("[UIManager] DailyRewardSystem.Instance null");
            return;
        }

        bool canClaim = DailyRewardSystem.Instance.CanClaimToday();
        Debug.Log($"[UIManager] CanClaimToday: {canClaim}");

        if (canClaim)
        {
            StartCoroutine(ShowDailyRewardsAfterDelay(1f));
        }
    }

    public void ShowDailyRewards()
    {
        if (_dailyRewardUI != null)
        {
            _dailyRewardUI.ShowDailyRewardPanel();
        }
    }

    IEnumerator ShowDailyRewardsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_dailyRewardUI == null)
        {
            Debug.LogError("[UIManager] _dailyRewardUI null. Asigna la referencia");
            yield break;
        }

        Debug.Log("[UIManager] Llamando ShowDailyRewardPanel");
        _dailyRewardUI.ShowDailyRewardPanel();
    }

    private void OnComboUpdated(int comboLevel)
    {
        _comboCountText.text = $"Combo: {comboLevel}";
        _comboCountText.gameObject.SetActive(true);

        if (comboLevel >= 2)
        {
            float bonus = comboLevel switch
            {
                2 => 10f,
                3 => 1.5f,
                4 => 2f,
                _ => 3f
            };
            _bonusTimeText.text = $"+{bonus:F0}s";
            _bonusTimeText.gameObject.SetActive(true);
            StartCoroutine(HideBonusCoroutine());
        }
    }

    private void OnComboEnded()
    {
        _comboCountText.gameObject.SetActive(false);
    }

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
    }

    public void OpenCanvas(Canvas canvas)
    {
        canvas.enabled = true;
    }

    public void CloseCanvas(Canvas canvas)
    {
        canvas.enabled = false;
    }

    public void ShowHideCanvas(Canvas canvas, bool state)
    {
        if (state)
        {
            OpenCanvas(canvas);
        }
        else
        {
            CloseCanvas(canvas);
        }
    }

    public void OpenCloseConfigPanel()
    {
        if (_isOpen)
        {
            _configPanelAnim.SetTrigger("Close");
            _isOpen = !_isOpen;
        }
        else
        {
            _configPanelAnim.SetTrigger("Open");
            _isOpen = !_isOpen;
        }
    }

    public void MusicOnOff()
    {
        _audioToggle.MusicButtonClicked();
    }

    public void SFXOnOff()
    {
        _audioToggle.SFXButtonClicked();
    }

    public void ShowHideCreditsCanvas()
    {
        bool isCanvasActive = !_creditsCanvas.enabled;
        ShowHideCanvas(_levelsCanvas, isCanvasActive);
        ShowHideCanvas(_creditsCanvas, isCanvasActive);
    }

    public void ShowHidePreGameCanvas()
    {
        bool isCanvasActive = !_preGameCanvas.enabled;
        ShowHideCanvas(_preGameCanvas, isCanvasActive);
        if (isCanvasActive)
            ShowPreGamePowerUps();
    }

    public void ShowPreGamePowerUps()
    {
        foreach (var slot in _slots)
            Destroy(slot.gameObject);
        _slots.Clear();

        var inventory = SaveManager.Instance.GetGameData().powerUpInventory;

        foreach (var powerUpBase in allPowerUpBases)
        {
            var item = inventory.Find(i => i.type == powerUpBase.powerUpType);
            if (item == null)
                item = new PowerUpInventoryItem(powerUpBase.powerUpType, 0);

            var slot = Instantiate(_powerUpSlotPrefab, _powerUpsPanel);
            slot.Setup(item, powerUpBase, OnPowerUpActivateClicked);
            _slots.Add(slot);
        }
    }

    private void OnPowerUpActivateClicked(PowerUpInventoryItem item)
    {
        if (item.quantity > 0)
        {
            item.quantity--;
            var powerUpBase = GetPowerUpBaseByType(item.type);
            var powerUpData = new PowerUpData
            {
                type = item.type,
                activationTime = DateTime.Now,
                duration = powerUpBase != null ? powerUpBase.duration : 3600f
            };
            SaveManager.Instance.GetGameData().activePowerUps.Add(powerUpData);
            SaveManager.Instance.SaveData();
            ShowPreGamePowerUps();
        }
    }

    private PowerUpBase GetPowerUpBaseByType(PowerUpType type)
    {
        foreach (var pu in allPowerUpBases)
            if (pu.powerUpType == type)
                return pu;
        Debug.LogWarning("No se encontró PowerUpBase para el tipo: " + type);
        return null;
    }

    public void ShowHidePauseCanvas()
    {
        bool isCanvasActive = !_pauseCanvas.enabled;
        ShowHideCanvas(_pauseCanvas, isCanvasActive);
        if (!isCanvasActive)
        {
            Time.timeScale = 1;
        }
        else
        {
            Time.timeScale = 0;
        }
    }

    public void ShowNoLivesPanel()
    {
        _noLivesPanel.SetActive(true);
        _noLivesActive = true;
    }

    public void HideNoLivesPanel()
    {
        _noLivesPanel.SetActive(false);
    }

    public void ShowLifeLostPanel()
    {
        _lifeLostPanel.SetActive(true);
    }

    public void HideLifeLostPanel()
    {
        _lifeLostPanel.SetActive(false);
    }

    public void UpdateLivesUI(int lives)
    {
        if (_livesText != null)
            _livesText.text = $"{lives}";
    }

    private void OnRetryPressed()
    {
        HideLifeLostPanel();

        if (!LifeManager.Instance.CanPlay())
        {
            ShowNoLivesPanel();
            return;
        }

        GameManager.Instance.RestartLevel();
    }

    private void OnBackToSelectionPressed()
    {
        HideLifeLostPanel();
        GameManager.Instance.GoToLevelSelection();
    }

    private void OnLivesChanged(int lives)
    {
        UpdateLivesUI(lives);

        if (_noLivesActive && lives > 0)
        {
            _noLivesActive = false;
            HideNoLivesPanel();
        }
    }

    public void ShowStarsDebug()
    {
        var data = SaveManager.Instance.GetGameData();
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Total estrellas: {data.totalStars}");

        foreach (var kvp in data.levelStars)
        {
            sb.AppendLine($"Nivel {kvp.Key}: {kvp.Value} estrellas");
        }

        debugStarsText.text = sb.ToString();
    }

    public void LoadDebugTestScene()
    {
        StartCoroutine(LoadDebugTestLevel());
    }

    public IEnumerator LoadDebugTestLevel()
    {
        _transitionAnim.SetTrigger("Start");
        yield return new WaitForSeconds(_transitionTime);
        _levelsCanvas.enabled = false;
        SceneManager.LoadScene("TestScene");
        _transitionAnim.SetTrigger("End");
        _gameplayCanvas.enabled = true;
    }

    public void UpdatePowerUpsUI()
    {
        var context = PowerUpManager.Instance?.context;
        if (_powerUpsText == null || context == null)
            return;

        string status = "";

        if (context.ExtraTimeActive)
            status += "Power up activo: Tiempo Extra\n";
        if (context.DashTurboActive)
            status += "Power up activo: Dash Turbo\n";
        if (context.ParryPerfectActive)
            status += "Power up activo: Parry Perfect\n";
        if (context.ComboMasterActive)
            status += "Power up activo: Combo Master\n";
        if (context.SecondChanceActive)
            status += "Power up activo: Second Chance\n";

        _powerUpsText.text = status.Length > 0 ? status : "Sin Power Ups activos";
    }

#if UNITY_EDITOR
    public void DeleteSaveDataFromUI()
    {
        SaveManager.Instance.DeleteSaveData();
        ShowStarsDebug();
        Debug.Log("[UIManager] Progreso borrado.");
    }
#endif
}
