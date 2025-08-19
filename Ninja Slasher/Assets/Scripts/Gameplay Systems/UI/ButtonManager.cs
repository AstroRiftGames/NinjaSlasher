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
    [SerializeField] private Button _heartButton;

    [Header("CONFIG DROPDOWN BUTTONS")]
    [SerializeField] private Button _musicButton;
    [SerializeField] private Button _sfxButton;
    [SerializeField] private Button _profileButton;

    [Header("PROFILE BUTTONS")]
    [SerializeField] private Button _userIconButton;

    [SerializeField] private Button _closeProfileButton;
    [SerializeField] private Button _creditsButton;
    [SerializeField] private Button _closeCreditsButton;

    [Header("EXTRA LIFE PANEL BUTTONS")]
    //[SerializeField] private Button _closeExtraLifeButton;
    //[SerializeField] private Button _claimExtraLifeButton;

    [Header("DAILY REWARDS BUTTONS")]
    [SerializeField] private Button _claimRewardButton;
    [SerializeField] private Button _closeCalendarButton;

    [Header("PREGAME BUTTONS")]
    [SerializeField] private Button _closePregameButton;
    [SerializeField] private Button _playButton;

    [Header("GAMEPLAY BUTTONS")]
    [SerializeField] private Button _pauseButton;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _backToSelectionButton;

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
    }

    private void OnDisable()
    {
        if (LevelProgressionManager.Instance != null)
        {
            LevelProgressionManager.Instance.OnProgressionUpdated -= RefreshLevelProgression;
        }
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
        SetupDailyRewardButtons();
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
        _quitButton.onClick.AddListener(UIManager.Instance.ShowLevelSelector);
        _retryButton.onClick.AddListener(GetComponent<GameplayUIManager>().OnRetryPressed);
        _backToSelectionButton.onClick.AddListener(GetComponent<GameplayUIManager>().OnBackToSelectionPressed);
    }

    private void SetupDailyRewardButtons()
    {
        _claimRewardButton.onClick.AddListener(() =>
        {
            if (DailyRewardSystem.Instance != null)
            {
                bool success = DailyRewardSystem.Instance.ClaimReward();
            }
        });

        _closeCalendarButton.onClick.AddListener(() =>
        {
            GetComponent<CanvasManager>().ShowHideDailyRewardCanvas();
        });
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

    private void SetupLevelSelectorButtons()
    {
        //EXTRA LIFE PANEL
        //_closeExtraLifeButton.onClick.AddListener(UIManager.Instance.ShowHideExtraLifeCanvas);
        //_claimExtraLifeButton.onClick.AddListener(UIManager.Instance.ShowHideExtraLifeCanvas);

        //CONFIG DROPDOWN
        _musicButton.onClick.AddListener(_audioToggle.MusicButtonClicked);
        _sfxButton.onClick.AddListener(_audioToggle.SFXButtonClicked);
        _profileButton.onClick.AddListener(UIManager.Instance.ShowHideProfileCanvas);

        //NO LIVES PANEL
        _closeNoLivesPanelButton.onClick.AddListener(UIManager.Instance.ShowHideNoLivesCanvas);
        _adForMoreLifeButton.onClick.AddListener(UIManager.Instance.ShowHideNoLivesCanvas);

        //PROFILE
        _userIconButton.onClick.AddListener(UIManager.Instance.ShowHideProfileCanvas);
        _creditsButton.onClick.AddListener(UIManager.Instance.ShowHideCreditsCanvas);
        _closeProfileButton.onClick.AddListener(UIManager.Instance.ShowHideProfileCanvas);
        _closeCreditsButton.onClick.AddListener(UIManager.Instance.ShowHideCreditsCanvas);

        //MAIN SCREEN
        _calendarButton.onClick.AddListener(UIManager.Instance.ShowHideDailyRewardCanvas);
        _configDropdownButton.onClick.AddListener(_configPanelManager.OpenCloseConfigPanel);
        _testLevelButton.onClick.AddListener(() => GetComponent<SceneTransitionManager>().LoadDebugTestScene());
        //_heartButton.onClick.AddListener(UIManager.Instance.ShowHideExtraLifeCanvas);

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
            if(star != null)
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
        Debug.Log($"[ButtonManager] Nivel {levelId} está bloqueado");
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
            bool wasInteractable = levelButtons[i].interactable;
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
        GameManager.Instance.RestartLevel();
        UIManager.Instance.ShowHidePauseCanvas();
    }

    private bool IsLevelUnlocked(int levelId)
    {
        if (LevelProgressionManager.Instance == null)
        {
            return levelId == 1;
        }

        bool isUnlocked = LevelProgressionManager.Instance.IsLevelUnlocked(levelId);
        return isUnlocked;
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