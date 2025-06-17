using System.Collections;
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
    [SerializeField] private GameObject _levelsPanel;
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


    private void Start()
    {
        SetButtonsUp();
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
    }

    IEnumerator ShowLevelSelectorCo()
    {
        _transitionAnim.SetTrigger("Start");
        yield return new WaitForSeconds(_transitionTime);
        _splashPanel.SetActive(false);
        _levelsPanel.SetActive(true);
        _pausePanel.SetActive(false);
        _transitionAnim.SetTrigger("End");
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
}
