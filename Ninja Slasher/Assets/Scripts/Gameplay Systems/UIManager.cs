using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviourSingleton<UIManager>
{
    [Header("TRANSITION ANIM")]
    [SerializeField] private Animator _transitionAnim;
    [SerializeField] private float _transitionTime;

    [Header("PANELS")]
    [SerializeField] private GameObject _splashPanel;
    [SerializeField] public GameObject _levelsPanel;
    [SerializeField] private GameObject _gameplayPanel;
    [SerializeField] private GameObject _creditsPanel;
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _configPanel;

    [Header("BUTTONS")]
    [SerializeField] Button _playButton;
    [SerializeField] Button _levelButton;
    [SerializeField] Button _creditsButton;
    [SerializeField] Button _configButton;
    [SerializeField] Button _pauseButton;
    [SerializeField] Button _resumeButton;
    [SerializeField] Button _restartButton;
    [SerializeField] Button _quitButton;

    [SerializeField] private TextMeshProUGUI _livesText;
    [SerializeField] private GameObject _noLivesPanel;
    [SerializeField] private TextMeshProUGUI _noLivesTimerText;
    [SerializeField] private TextMeshProUGUI _comboCountText;
    [SerializeField] private TextMeshProUGUI _levelTimerText;
    [SerializeField] private TextMeshProUGUI _bonusTimeText;

    private bool _noLivesActive = false;
    private LevelController _levelController;
    private ComboManager _comboManager;

    private void Start()
    {
        SetButtonsUp();
        if (LifeManager.Instance != null)
            LifeManager.Instance.OnLivesChanged += OnLivesChanged;

        UpdateLivesUI(LifeManager.Instance.CurrentLives);
        _comboCountText.gameObject.SetActive(false);
        _bonusTimeText.gameObject.SetActive(false);

        ShowLevelSelector();
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
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_levelController != null)
        {
            _levelController.OnTimeChanged -= OnLevelTimeChanged;
            _levelController.OnTimeExpired -= OnLevelTimeExpired;
        }

        _levelController = FindObjectOfType<LevelController>();
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

    private void SetButtonsUp()
    {
        _playButton.onClick.AddListener(ShowLevelSelector);
        _levelButton.onClick.AddListener(LoadNextLevelScene);
        _creditsButton.onClick.AddListener(ShowHideCreditsPanel);
        _configButton.onClick.AddListener(ShowHideConfigPanel);
        _pauseButton.onClick.AddListener(ShowHidePausePanel);
        _resumeButton.onClick.AddListener(ShowHidePausePanel);
        _restartButton.onClick.AddListener(RestartLevel);
        _quitButton.onClick.AddListener(ShowLevelSelector);
    }

    public void LoadNextLevelScene()
    {
        StartCoroutine(LoadNextLevelSceneCo(SceneManager.GetActiveScene().buildIndex + 1));
    }

    IEnumerator LoadNextLevelSceneCo(int levelIndex)
    {
        _transitionAnim.SetTrigger("Start");
        yield return new WaitForSeconds(_transitionTime);
        _levelsPanel.SetActive(false);
        SceneManager.LoadScene(levelIndex);
        _transitionAnim.SetTrigger("End");
        _gameplayPanel.SetActive(true);
    }

    public void RestartLevel()
    {
        StartCoroutine(LoadNextLevelSceneCo(SceneManager.GetActiveScene().buildIndex));
        ShowHidePausePanel();
    }

    public void ShowLevelSelector()
    {
        StartCoroutine(ShowLevelSelectorCo());
        UpdateLivesUI(LifeManager.Instance.CurrentLives);
    }

    IEnumerator ShowLevelSelectorCo()
    {
        _transitionAnim.SetTrigger("Start");
        yield return new WaitForSeconds(_transitionTime);
        _splashPanel.SetActive(false);
        _levelsPanel.SetActive(true);
        _pausePanel.SetActive(false);
        if (!LifeManager.Instance.CanPlay())
            ShowNoLivesPanel();

        _transitionAnim.SetTrigger("End");
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

    public void OpenPanel(GameObject panel)
    {
        panel.SetActive(true);
    }

    public void ClosePanel(GameObject panel)
    {
        panel.SetActive(false);
    }

    public void ShowHidePanel(GameObject panel, bool state)
    {
        if (state)
        {
            OpenPanel(panel);
        }
        else
        {
            ClosePanel(panel);
        }
    }

    public void ShowHideConfigPanel()
    {
        bool isPanelActive = !_configPanel.activeInHierarchy;
        ShowHidePanel(_splashPanel, isPanelActive);
        ShowHidePanel(_configPanel, isPanelActive);
    }

    public void ShowHideCreditsPanel()
    {
        bool isPanelActive = !_creditsPanel.activeInHierarchy;
        ShowHidePanel(_splashPanel, isPanelActive);
        ShowHidePanel(_creditsPanel, isPanelActive);
    }

    public void ShowHidePausePanel()
    {
        bool isPanelActive = !_pausePanel.activeInHierarchy;
        ShowHidePanel(_pausePanel, isPanelActive);
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

    public void UpdateLivesUI(int lives)
    {
        if (_livesText != null)
            _livesText.text = $"Vidas: {lives}";
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
}
