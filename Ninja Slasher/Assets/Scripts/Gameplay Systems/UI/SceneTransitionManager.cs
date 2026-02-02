using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("SCREEN TRANSITION")]
    [SerializeField] private Animator _transitionAnim;
    [SerializeField] private float _transitionTime;

    [SerializeField] private GameObject _hudObject;

    private UIAudioContext _audioContext;

    private void Awake()
    {
        _audioContext = GetComponentInParent<UIAudioContext>();
    }

    private void OnEnable()
    {
        UIEvents.OnSceneTransitionRequested += LoadLevelScene;
        UIEvents.OnRestartLevelRequested += RestartLevel;
        UIEvents.OnShowLevelSelectorRequested += ShowLevelSelector;
    }

    private void OnDisable()
    {
        UIEvents.OnSceneTransitionRequested -= LoadLevelScene;
        UIEvents.OnRestartLevelRequested -= RestartLevel;
        UIEvents.OnShowLevelSelectorRequested -= ShowLevelSelector;
    }

    public void LoadLevelScene(string sceneName)
    {
        StartCoroutine(LoadLevelSceneCo(sceneName));
    }

    private IEnumerator LoadLevelSceneCo(string sceneName)
    {
        SetHUDActive(false);

        if (AudioService.Instance != null)
        {
            AudioService.Instance.StopAllSFX();
        }

        _transitionAnim.SetTrigger("Start");
        yield return new WaitForSeconds(_transitionTime);

        UIManager.Instance.SetLevelsScreenEnabled(false);

        SceneManager.LoadScene(sceneName);

        _transitionAnim.SetTrigger("End");
        AudioService.Instance?.PlaySFX(_audioContext.Audio.transitionSlash);

        UIManager.Instance.SetGameplayHUDEnabled(true);

        yield return new WaitForEndOfFrame();
        SetHUDActive(true);
    }

    public void RestartLevel()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        LoadLevelScene(sceneName);
    }

    public void ShowLevelSelector()
    {
        StartCoroutine(ShowLevelSelectorCo());
    }

    private IEnumerator ShowLevelSelectorCo()
    {
        Time.timeScale = 1;

        SetHUDActive(false);

        UIEvents.RequestHideVictoryModal();
        UIEvents.RequestHidePauseOverlay();
        UIEvents.RequestHideNoLivesOverlay();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideDefeatOverlay();
        }

        if (AudioService.Instance != null)
        {
            AudioService.Instance.StopAllSFX();
        }

        _transitionAnim.SetTrigger("OpeningStart");
        yield return new WaitForSeconds(_transitionTime);

        UIEvents.RequestHideSplashScreen();
        UIManager.Instance.SetLevelsScreenEnabled(true);
        UIManager.Instance.SetGameplayHUDEnabled(false);

        _transitionAnim.SetTrigger("End");
        AudioService.Instance?.PlaySFX(_audioContext.Audio.transitionSlash);

        MusicEvents.OnEnterLevelSelection?.Invoke();

        UIEvents.RaiseLevelSelectorReady();
    }

    private void SetHUDActive(bool active)
    {
        if (_hudObject != null)
        {
            _hudObject.SetActive(active);
        }
    }
}