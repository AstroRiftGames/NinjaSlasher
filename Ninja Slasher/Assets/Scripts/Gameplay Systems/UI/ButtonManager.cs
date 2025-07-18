using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    [Header("LEVEL SELECTOR BUTTONS")]
    [SerializeField] private Button[] levelButtons;
    [SerializeField] private Button _testLevelButton;
    [SerializeField] private Button _configButton;
    [SerializeField] private Button _musicButton;
    [SerializeField] private Button _sfxButton;
    [SerializeField] private Button _creditsButton;

    [Header("PREGAME BUTTONS")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _playButton;

    [Header("GAMEPLAY BUTTONS")]
    [SerializeField] private Button _pauseButton;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _backToSelectionButton;

    [Header("PROGRESSION UI")]
    [SerializeField] private Image[] levelButtonImages;
    [SerializeField] private Color lockedButtonColor = Color.gray;
    [SerializeField] private Color unlockedButtonColor = Color.white;

    [SerializeField] private string[] sceneNames;

#if UNITY_EDITOR
    [SerializeField] private Button deleteSaveButton;
#endif

    private AudioToggle _audioToggle;
    private ConfigPanelManager _configPanelManager;

    private void Awake()
    {
        _audioToggle = GetComponent<AudioToggle>();
        _configPanelManager = GetComponent<ConfigPanelManager>();
    }

    private void OnEnable()
    {
        if (LevelProgressionManager.Instance != null)
        {
            LevelProgressionManager.Instance.OnProgressionUpdated += RefreshLevelProgression;
        }
    }

    private void OnDisable()
    {
        if (LevelProgressionManager.Instance != null)
        {
            LevelProgressionManager.Instance.OnProgressionUpdated -= RefreshLevelProgression;
        }
    }

    public void SetupButtons()
    {
        SetupLevelSelectorButtons();
        SetupPreGameButtons();
        SetupGameplayButtons();

        SetupLevelProgression();

#if UNITY_EDITOR
        SetupDebugButtons();
#endif
    }

    private void SetupPreGameButtons()
    {
        _closeButton.onClick.AddListener(UIManager.Instance.ShowHidePreGameCanvas);
        _playButton.onClick.AddListener(UIManager.Instance.ShowHidePreGameCanvas);
    }

    private void SetupGameplayButtons()
    {
        _pauseButton.onClick.AddListener(UIManager.Instance.ShowHidePauseCanvas);
        _resumeButton.onClick.AddListener(UIManager.Instance.ShowHidePauseCanvas);
        _restartButton.onClick.AddListener(UIManager.Instance.RestartLevel);
        _quitButton.onClick.AddListener(UIManager.Instance.ShowLevelSelector);
        _retryButton.onClick.AddListener(GetComponent<GameplayUIManager>().OnRetryPressed);
        _backToSelectionButton.onClick.AddListener(GetComponent<GameplayUIManager>().OnBackToSelectionPressed);
    }

    private void SetupLevelProgression()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelId = i + 1;
            bool isUnlocked = IsLevelUnlocked(levelId);

            levelButtons[i].interactable = isUnlocked;

            if (levelButtonImages != null && i < levelButtonImages.Length && levelButtonImages[i] != null)
            {
                levelButtonImages[i].color = isUnlocked ? unlockedButtonColor : lockedButtonColor;
            }

            Debug.Log($"[ButtonManager] Nivel {levelId}: {(isUnlocked ? "DESBLOQUEADO" : "BLOQUEADO")}");
        }
    }

    private void SetupLevelSelectorButtons()
    {
        _configButton.onClick.AddListener(_configPanelManager.OpenCloseConfigPanel);
        _musicButton.onClick.AddListener(_audioToggle.MusicButtonClicked);
        _sfxButton.onClick.AddListener(_audioToggle.SFXButtonClicked);
        _creditsButton.onClick.AddListener(GetComponent<CanvasManager>().ShowHideCreditsCanvas);
        _testLevelButton.onClick.AddListener(() => GetComponent<SceneTransitionManager>().LoadDebugTestScene());

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelId = i + 1;
            string sceneName = sceneNames[i];

            levelButtons[i].onClick.AddListener(() =>
            {
                if (IsLevelUnlocked(levelId))
                {
                    UIManager.Instance.ShowConfirmationPanel(sceneName);
                }
                else
                {
                    ShowLevelLockedMessage(levelId);
                }
            });
        }
    }

    private void ShowLevelLockedMessage(int levelId)
    {
        Debug.Log($"[ButtonManager] Nivel {levelId} está bloqueado");
    }

    public void RefreshLevelProgression()
    {
        SetupLevelProgression();
    }

    private bool IsLevelUnlocked(int levelId)
    {
        if (LevelProgressionManager.Instance == null)
        {
            Debug.LogWarning("[BUTTON MANAGER] LevelProgressionManager no encontrado");
            return true;
        }

        return LevelProgressionManager.Instance.IsLevelUnlocked(levelId);
    }

#if UNITY_EDITOR
    private void SetupDebugButtons()
    {
        if (deleteSaveButton != null)
        {
            deleteSaveButton.onClick.AddListener(() =>
            {
                GetComponent<DebugUIManager>().DeleteSaveDataFromUI();
                RefreshLevelProgression();
            });
        }
    }
#endif
}