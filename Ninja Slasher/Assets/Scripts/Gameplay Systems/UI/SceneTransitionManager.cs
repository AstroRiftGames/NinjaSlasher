using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("SCREEN TRANSITION")]
    [SerializeField] private Animator _transitionAnim;
    [SerializeField] private float _transitionTime;
    [SerializeField] private string _levelSelectorSceneName = "SplashScreen";

    [Header("KATANA TRANSITION")]
    [SerializeField] private KatanaTransitionController _katanaTransition;

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
        if (HasKatanaTransition())
        {
            StartCoroutine(LoadLevelSceneWithKatanaCo(sceneName));
            return;
        }

        StartCoroutine(LoadLevelSceneLegacyCo(sceneName));
    }

    private bool HasKatanaTransition()
    {
        return _katanaTransition != null && _katanaTransition.IsReady;
    }

    private IEnumerator LoadLevelSceneWithKatanaCo(string sceneName)
    {
        BeginTransitionPause();
        SetHUDActive(false);

        if (AudioService.Instance != null)
        {
            AudioService.Instance.StopAllSFX();
        }

        UIManager.Instance?.SetLevelsScreenEnabled(false);

        _katanaTransition.PlayEnterLevelTransition();
        yield return WaitForKatanaTransitionToComplete();

        SceneManager.LoadScene(sceneName);
        yield return WaitForScenePresentationFrame();

        _katanaTransition.PlayExitLevelTransition();
        yield return WaitForKatanaTransitionToComplete();

        UIManager.Instance?.SetGameplayHUDEnabled(true);
        EndTransitionPause();

        yield return new WaitForEndOfFrame();
        SetHUDActive(true);
    }

    private IEnumerator LoadLevelSceneLegacyCo(string sceneName)
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

        if (HasKatanaTransition())
        {
            StartCoroutine(LoadLevelSelectorSceneWithKatanaCo());
            return;
        }

        StartCoroutine(LoadLevelSelectorSceneLegacyCo());
    }

    private IEnumerator LoadLevelSelectorSceneWithKatanaCo()
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

        _katanaTransition.PlayEnterLevelTransition();
        yield return WaitForKatanaTransitionToComplete();

        MusicEvents.OnEnterLevelSelection?.Invoke();
        
        SceneManager.sceneLoaded += OnLevelSelectorSceneLoaded;
        SceneManager.LoadScene(_levelSelectorSceneName);
    }

    private IEnumerator LoadLevelSelectorSceneLegacyCo()
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
        if (HasKatanaTransition())
        {
            StartCoroutine(ShowLevelSelectorWithKatanaCo());
            return;
        }

        StartCoroutine(ShowLevelSelectorLegacyCo());
    }

    private IEnumerator ShowLevelSelectorWithKatanaCo()
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

        _katanaTransition.PlayEnterLevelTransition();
        yield return WaitForKatanaTransitionToComplete();

        UIEvents.RequestHideSplashScreen();
        UIManager.Instance.SetLevelsScreenEnabled(true);
        UIManager.Instance.SetGameplayHUDEnabled(false);

        _katanaTransition.PlayExitLevelTransition();
        yield return WaitForKatanaTransitionToComplete();

        UIEvents.RequestUpdateLivesUI(LifeManager.Instance?.CurrentLives ?? 0);
        UIEvents.RaiseLevelSelectorReady();
    }

    private IEnumerator ShowLevelSelectorLegacyCo()
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

        if (HasKatanaTransition())
        {
            _katanaTransition.PlayExitLevelTransition();
            yield return WaitForKatanaTransitionToComplete();
        }
        else
        {
            _transitionAnim.SetTrigger("End");
            AudioService.Instance?.PlaySFX(_audioContext.Audio.transitionSlash);
        }

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

    private IEnumerator WaitForKatanaTransitionToComplete()
    {
        if (_katanaTransition == null)
        {
            yield break;
        }

        while (_katanaTransition.IsTransitioning)
        {
            yield return null;
        }
    }

    private static IEnumerator WaitForScenePresentationFrame()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
    }

    private void BeginTransitionPause()
    {
        PauseController.Instance?.RequestPause(PauseSource.Transition);
    }

    private void EndTransitionPause()
    {
        PauseController.Instance?.ReleasePause(PauseSource.Transition);
    }
}
