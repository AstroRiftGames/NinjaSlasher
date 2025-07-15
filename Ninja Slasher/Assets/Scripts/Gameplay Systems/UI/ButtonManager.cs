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

    public void SetupButtons()
    {
        SetupLevelSelectorButtons();
        SetupPreGameButtons();
        SetupGameplayButtons();
#if UNITY_EDITOR
        SetupDebugButtons();
#endif
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
            string sceneName = sceneNames[i];
            levelButtons[i].onClick.AddListener(() => UIManager.Instance.ShowConfirmationPanel(sceneName));
        }
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

#if UNITY_EDITOR
    private void SetupDebugButtons()
    {
        if (deleteSaveButton != null)
            deleteSaveButton.onClick.AddListener(GetComponent<DebugUIManager>().DeleteSaveDataFromUI);
    }
#endif
}