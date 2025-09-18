using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    [Header("LEVEL SELECTOR BUTTONS")]
    [SerializeField] private Button[] levelButtons;
    [SerializeField] private Button _testLevelButton;
    [SerializeField] private Button _configDropdownButton;
    [SerializeField] private Button _calendarButton;

    [Header("CONFIG DROPDOWN BUTTONS")]
    [SerializeField] private Button _musicButton;
    [SerializeField] private Button _sfxButton;
    [SerializeField] private Button _profileButton;

    [Header("PROFILE BUTTONS")]
    [SerializeField] private Button _userIconButton;
    [SerializeField] private Button _userNicknameButton;
    [SerializeField] private string _userNicknameText;
    [SerializeField] private Button _closeProfileButton;
    [SerializeField] private Button _creditsButton;
    [SerializeField] private Button _closeCreditsButton;
    [SerializeField] private Image _userIconImage;
    [SerializeField] private Button[] _userIconButtonGroup;

    [Header("DAILY REWARDS BUTTONS")]
    [SerializeField] private Button _closeCalendarButton;

    [Header("PREGAME BUTTONS")]
    [SerializeField] private Button _closePregameButton;
    [SerializeField] private Button _playButton;

    [Header("GAMEPLAY BUTTONS")]
    [SerializeField] private Button _pauseButton;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _musicPausePanelButton;
    [SerializeField] private Button _sfxPausePanelButton;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _backToSelectionButton;
    [SerializeField] private Button _continueButton;

    [Header("NO LIVES PANEL BUTTONS")]
    [SerializeField] private Button _closeNoLivesPanelButton;
    [SerializeField] private Button _adForMoreLifeButton;

    [Header("PROGRESSION UI")]
    [SerializeField] private Image[] levelButtonImages;
    [SerializeField] private Color lockedButtonColor = Color.gray;
    [SerializeField] private Color unlockedButtonColor = Color.white;

    [SerializeField] private string[] sceneNames;

    [SerializeField] private Color _starNotAcquired = Color.black;
    [SerializeField] private Color _starAcquired = Color.yellow;

#if UNITY_EDITOR
    [SerializeField] private Button deleteSaveButton;
#endif

    private AudioToggle _audioToggle;
    private ConfigDropdown _configPanelManager;

    private void OnSaveDataLoaded(GameData _) => RefreshLevelProgression();

    private void Awake()
    {
        _audioToggle = GetComponent<AudioToggle>();
        _configPanelManager = GetComponent<ConfigDropdown>();
    }

    private void OnEnable()
    {
        if (LevelProgressionManager.Instance != null)
        {
            LevelProgressionManager.Instance.OnProgressionUpdated += RefreshLevelProgression;
        }
        else
        {
            StartCoroutine(DelayedSubscription());
        }

        SaveManager.OnDataLoaded += OnSaveDataLoaded;
    }

    private void OnDisable()
    {
        if (LevelProgressionManager.Instance != null)
        {
            LevelProgressionManager.Instance.OnProgressionUpdated -= RefreshLevelProgression;
        }

        SaveManager.OnDataLoaded -= OnSaveDataLoaded;
    }

    private IEnumerator DelayedSubscription()
    {
        yield return null;

        if (LevelProgressionManager.Instance != null)
        {
            LevelProgressionManager.Instance.OnProgressionUpdated += RefreshLevelProgression;
        }
    }

    public void SetupButtons()
    {
        SetupLevelSelectorButtons();
        SetupPreGameButtons();
        SetupGameplayButtons();
        SetupLevelProgression();
        UpdateButtonProgression();

#if UNITY_EDITOR
        SetupDebugButtons();
#endif
    }

    private void SetupPreGameButtons()
    {
        _closePregameButton.onClick.AddListener(UIManager.Instance.ShowHidePreGameCanvas);
        _playButton.onClick.AddListener(UIManager.Instance.ShowHidePreGameCanvas);
    }

    private void SetupGameplayButtons()
    {
        _pauseButton.onClick.AddListener(UIManager.Instance.ShowHidePauseCanvas);
        _resumeButton.onClick.AddListener(UIManager.Instance.ShowHidePauseCanvas);
        _restartButton.onClick.AddListener(OnRestartPressed);

        _quitButton.onClick.AddListener(() => LevelManager.Instance.GoToLevelSelection(confirmPendingDeduction: true));

        _musicPausePanelButton.onClick.AddListener(_audioToggle.MusicButtonClicked);
        _sfxPausePanelButton.onClick.AddListener(_audioToggle.SFXButtonClicked);

        _retryButton.onClick.AddListener(GetComponent<GameplayUIManager>().OnRetryPressed);
        _backToSelectionButton.onClick.AddListener(GetComponent<GameplayUIManager>().OnBackToSelectionPressed);
        _continueButton.onClick.AddListener(GetComponent<GameplayUIManager>().ContinueToLevelSelector);
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
        }
    }

    public void OpenURLButtonClicked(string url)
    {
        UIManager.Instance.OpenURL(url);
    }

    private void SetupLevelSelectorButtons()
    {
        _musicButton.onClick.AddListener(_audioToggle.MusicButtonClicked);
        _sfxButton.onClick.AddListener(_audioToggle.SFXButtonClicked);
        _profileButton.onClick.AddListener(UIManager.Instance.ShowHideProfileCanvas);

        _closeNoLivesPanelButton.onClick.AddListener(UIManager.Instance.ShowHideNoLivesCanvas);
        _adForMoreLifeButton.onClick.AddListener(AdsManager.Instance.ShowRewardedAdForExtraLife);

        _userIconButton.onClick.AddListener(UIManager.Instance.ShowHideUserIconsCanvas);
        _userNicknameText = LoginManager.Instance.PlayerName;
        _userNicknameButton.onClick.AddListener(UIManager.Instance.ShowHideUserNicknameEditCanvas);
        _creditsButton.onClick.AddListener(UIManager.Instance.ShowHideCreditsCanvas);
        _closeProfileButton.onClick.AddListener(UIManager.Instance.ShowHideProfileCanvas);
        _closeCreditsButton.onClick.AddListener(UIManager.Instance.ShowHideCreditsCanvas);

        foreach (var img in _userIconButtonGroup)
        {
            img.onClick.AddListener(() =>
            {
                Image icon = img.transform.GetChild(0).GetComponent<Image>();
                _userIconImage.sprite = icon.sprite;
                _userIconImage.color = icon.color;  //SE NECESITA SPRITE CON EL COLOR PARA QUITAR ESTO
                UIManager.Instance.ShowHideUserIconsCanvas();
            });
        }

        _calendarButton.onClick.AddListener(UIManager.Instance.ShowHideDailyRewardCanvas);
        _configDropdownButton.onClick.AddListener(_configPanelManager.OpenCloseConfigPanel);
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

    void UpdateStars(Button levelButton, int levelId)
    {
        Transform buttonTransform = levelButton.transform;
        Transform starsContainer = buttonTransform.Find("Stars");
        int starsEarned = GetStars(levelId);
        bool isLevelUnlocked = IsLevelUnlocked(levelId);

        for (int i = 0; i < 3; i++)
        {
            Transform star = starsContainer.GetChild(i);
            if (star != null)
            {
                Image starImage = star.GetComponent<Image>();
                if (starImage != null)
                {
                    star.gameObject.SetActive(isLevelUnlocked);
                    if (isLevelUnlocked)
                    {
                        bool isEarned = i < starsEarned;
                        starImage.color = isEarned ? _starAcquired : _starNotAcquired;
                    }
                }
            }
        }
    }

    private int GetStars(int levelId)
    {
        var gameData = SaveManager.Instance.GetGameData();
        if (gameData.levelStars.TryGetValue(levelId, out int stars))
        {
            return stars;
        }
        return 0;
    }

    void UpdateButtonProgression()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelId = i + 1;
            UpdateStars(levelButtons[i], levelId);
        }
    }

    private void ShowLevelLockedMessage(int levelId)
    {
        Debug.Log($"[ButtonManager] Nivel {levelId} est� bloqueado");
    }

    public void RefreshLevelProgression()
    {
        var progressionInfo = LevelProgressionManager.Instance?.GetProgressionInfo();
        if (progressionInfo == null)
        {
            return;
        }

        int unlockedCount = 0;
        int lockedCount = 0;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelId = i + 1;
            bool isUnlocked = IsLevelUnlocked(levelId);

            levelButtons[i].interactable = isUnlocked;

            if (levelButtonImages != null && i < levelButtonImages.Length && levelButtonImages[i] != null)
            {
                levelButtonImages[i].color = isUnlocked ? unlockedButtonColor : lockedButtonColor;
            }

            if (isUnlocked) unlockedCount++;
            else lockedCount++;
        }

        UpdateButtonProgression();
    }

    private void OnRestartPressed()
    {
        LevelManager.Instance.RestartLevel();
        UIManager.Instance.ShowHidePauseCanvas();
    }

    private bool IsLevelUnlocked(int levelId)
    {
        if (LevelProgressionManager.Instance == null)
        {
            return levelId == 1;
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
