using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("SCREEN TRANSITION")]
    [SerializeField] private Animator _transitionAnim;
    [SerializeField] private float _transitionTime;
    [SerializeField] private string _levelSelectorSceneName = "SplashScreen";

    [SerializeField] private GameObject _hudObject;

    private UIAudioContext _audioContext;
    private DailyStartupSequence _dailyStartupSequence;
    private bool _isLoadingLevelSelectorScene;

    private void Awake()
    {
        _audioContext = GetComponentInParent<UIAudioContext>();
        _dailyStartupSequence = new DailyStartupSequence(this);
    }

    private void OnDestroy()
    {
        _dailyStartupSequence?.Dispose();
    }

    private void OnEnable()
    {
        UIEvents.OnSceneTransitionRequested += LoadLevelScene;
        UIEvents.OnLoadLevelSelectorSceneRequested += LoadLevelSelectorScene;
        UIEvents.OnShowLevelSelectorRequested += ShowLevelSelector;
    }

    private void OnDisable()
    {
        UIEvents.OnSceneTransitionRequested -= LoadLevelScene;
        UIEvents.OnLoadLevelSelectorSceneRequested -= LoadLevelSelectorScene;
        UIEvents.OnShowLevelSelectorRequested -= ShowLevelSelector;
    }

    private void LoadLevelScene(string sceneName)
    {
        StartCoroutine(LoadLevelSceneCo(sceneName));
    }

    private IEnumerator LoadLevelSceneCo(string sceneName)
    {
        SetHUDActive(false);
        UIManager.Instance.SetLevelsScreenEnabled(false);

        if (AudioService.Instance != null)
        {
            AudioService.Instance.StopAllSFX();
        }

        _transitionAnim.SetTrigger("Start");
        yield return new WaitForSeconds(_transitionTime);

        SceneManager.LoadScene(sceneName);

        _transitionAnim.SetTrigger("End");
        AudioService.Instance?.PlaySFX(_audioContext.Audio.transitionSlash);

        UIManager.Instance.SetGameplayHUDEnabled(true);

        yield return new WaitForEndOfFrame();
        SetHUDActive(true);
    }

    public void LoadLevelSelectorScene()
    {
        if (_isLoadingLevelSelectorScene)
            return;

        StartCoroutine(LoadLevelSelectorSceneCo());
    }

    private IEnumerator LoadLevelSelectorSceneCo()
    {
        _isLoadingLevelSelectorScene = true;

        SetHUDActive(false);

        UIEvents.RequestHideVictoryModal();
        UIEvents.RequestHidePauseOverlay();
        UIEvents.RequestHideNoLivesModal();
        UIEvents.RequestHideDefeatModal();
        UIEvents.RequestHideEmergencyBundleModal();
        UIEvents.RequestHidePregameModal();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetGameplayHUDEnabled(false);
            UIManager.Instance.SetLevelsScreenEnabled(false);
            UIManager.Instance.ResetLevelsScreenAnimation();
        }

        if (AudioService.Instance != null)
        {
            AudioService.Instance.StopAllSFX();
        }

        _transitionAnim.SetTrigger("OpeningStart");
        yield return new WaitForSecondsRealtime(_transitionTime);

        MusicEvents.OnEnterLevelSelection?.Invoke();
        
        SceneManager.sceneLoaded += OnLevelSelectorSceneLoaded;
        SceneManager.LoadScene(_levelSelectorSceneName);
    }

    public void ShowLevelSelector()
    {
        StartCoroutine(ShowLevelSelectorCo());
    }

    private IEnumerator ShowLevelSelectorCo()
    {
        SetHUDActive(false);

        UIEvents.RequestHideVictoryModal();
        UIEvents.RequestHidePauseOverlay();
        UIEvents.RequestHideNoLivesModal();
        UIEvents.RequestHideDefeatModal();
        UIEvents.RequestHideEmergencyBundleModal();
        UIEvents.RequestHidePregameModal();

        if (AudioService.Instance != null)
        {
            AudioService.Instance.StopAllSFX();
        }

        _transitionAnim.SetTrigger("OpeningStart");
        yield return new WaitForSecondsRealtime(_transitionTime);

        UIEvents.RequestHideSplashScreen();
        UIManager.Instance.SetLevelsScreenEnabled(true);
        UIManager.Instance.SetGameplayHUDEnabled(false);

        _transitionAnim.SetTrigger("End");
        AudioService.Instance?.PlaySFX(_audioContext.Audio.transitionSlash);

        UIEvents.RequestUpdateLivesUI(LifeManager.Instance?.CurrentLives ?? 0);

        UIEvents.RaiseLevelSelectorReady();
    }

    private void OnLevelSelectorSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != _levelSelectorSceneName)
            return;

        SceneManager.sceneLoaded -= OnLevelSelectorSceneLoaded;
        StartCoroutine(FinalizeLevelSelectorAfterSceneLoad());
    }

    private IEnumerator FinalizeLevelSelectorAfterSceneLoad()
    {
        yield return null;

        UIEvents.RequestHideSplashScreen();
        UIManager.Instance?.SetLevelsScreenEnabled(true);
        UIManager.Instance?.SetGameplayHUDEnabled(false);

        _transitionAnim.SetTrigger("End");
        AudioService.Instance?.PlaySFX(_audioContext.Audio.transitionSlash);

        UIEvents.RequestUpdateLivesUI(LifeManager.Instance?.CurrentLives ?? 0);

        UIEvents.RaiseLevelSelectorReady();
        _isLoadingLevelSelectorScene = false;
    }

    private void SetHUDActive(bool active)
    {
        if (_hudObject != null)
        {
            _hudObject.SetActive(active);
        }
    }
}
