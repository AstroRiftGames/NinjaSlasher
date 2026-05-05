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
    private bool _isSceneTransitionInProgress;

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

        EndSceneTransition();
    }

    private void LoadLevelScene(string sceneName)
    {
        if (!TryBeginSceneTransition())
            return;

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
        UIManager uiManager = UIManager.Instance;

        BeginTransitionPause();
        SetHUDActive(false);

        if (AudioService.Instance != null)
        {
            AudioService.Instance.StopAllSFX();
        }

        if (uiManager != null)
            yield return uiManager.HideActivePanelsForSceneTransition(hideLevelsScreen: true);

        _katanaTransition.PlayEnterLevelTransition();
        yield return WaitForKatanaTransitionToComplete();

        SceneManager.LoadScene(sceneName);
        yield return WaitForScenePresentationFrame();

        _katanaTransition.PlayExitLevelTransition();
        yield return WaitForKatanaTransitionToComplete();

        EndTransitionPause();
        EndSceneTransition();
        uiManager?.SetGameplayHUDEnabled(true);

        yield return new WaitForEndOfFrame();
        SetHUDActive(true);
    }

    private IEnumerator LoadLevelSceneLegacyCo(string sceneName)
    {
        UIManager uiManager = UIManager.Instance;

        BeginTransitionPause();
        SetHUDActive(false);

        if (AudioService.Instance != null)
        {
            AudioService.Instance.StopAllSFX();
        }

        if (uiManager != null)
            yield return uiManager.HideActivePanelsForSceneTransition(hideLevelsScreen: true);

        _transitionAnim.SetTrigger("Start");
        yield return new WaitForSecondsRealtime(_transitionTime);

        SceneManager.LoadScene(sceneName);

        _transitionAnim.SetTrigger("End");
        AudioService.Instance?.PlaySFX(_audioContext.Audio.transitionSlash);

        EndTransitionPause();
        EndSceneTransition();
        uiManager?.SetGameplayHUDEnabled(true);

        yield return new WaitForEndOfFrame();
        SetHUDActive(true);
    }

    public void LoadLevelSelectorScene()
    {
        if (_isLoadingLevelSelectorScene || !TryBeginSceneTransition())
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
        UIManager uiManager = UIManager.Instance;

        SetHUDActive(false);

        if (uiManager != null)
        {
            uiManager.ResetLevelsScreenAnimation();
        }

        if (AudioService.Instance != null)
        {
            AudioService.Instance.StopAllSFX();
        }

        if (uiManager != null)
            yield return uiManager.HideActivePanelsForSceneTransition();

        _katanaTransition.PlayEnterLevelTransition();
        yield return WaitForKatanaTransitionToComplete();

        MusicEvents.OnEnterLevelSelection?.Invoke();
        
        SceneManager.sceneLoaded += OnLevelSelectorSceneLoaded;
        SceneManager.LoadScene(_levelSelectorSceneName);
    }

    private IEnumerator LoadLevelSelectorSceneLegacyCo()
    {
        _isLoadingLevelSelectorScene = true;
        UIManager uiManager = UIManager.Instance;

        SetHUDActive(false);

        if (uiManager != null)
        {
            uiManager.ResetLevelsScreenAnimation();
        }

        if (AudioService.Instance != null)
        {
            AudioService.Instance.StopAllSFX();
        }

        if (uiManager != null)
            yield return uiManager.HideActivePanelsForSceneTransition();

        _transitionAnim.SetTrigger("OpeningStart");
        yield return new WaitForSecondsRealtime(_transitionTime);

        MusicEvents.OnEnterLevelSelection?.Invoke();
        
        SceneManager.sceneLoaded += OnLevelSelectorSceneLoaded;
        SceneManager.LoadScene(_levelSelectorSceneName);
    }

    public void ShowLevelSelector()
    {
        if (!TryBeginSceneTransition())
            return;

        if (HasKatanaTransition())
        {
            StartCoroutine(ShowLevelSelectorWithKatanaCo());
            return;
        }

        StartCoroutine(ShowLevelSelectorLegacyCo());
    }

    private IEnumerator ShowLevelSelectorWithKatanaCo()
    {
        UIManager uiManager = UIManager.Instance;

        SetHUDActive(false);

        if (AudioService.Instance != null)
        {
            AudioService.Instance.StopAllSFX();
        }

        if (uiManager != null)
        {
            yield return uiManager.HideActivePanelsForSceneTransition();
        }

        _katanaTransition.PlayEnterLevelTransition();
        yield return WaitForKatanaTransitionToComplete();

        if (uiManager != null)
        {
            yield return uiManager.SetSplashScreenVisibilityForTransition(false);
            yield return uiManager.SetLevelsScreenVisibilityForTransition(true);
        }

        _katanaTransition.PlayExitLevelTransition();
        yield return WaitForKatanaTransitionToComplete();

        UIEvents.RequestUpdateLivesUI(LifeManager.Instance?.CurrentLives ?? 0);
        EndSceneTransition();
        uiManager?.SetGameplayHUDEnabled(false);
        UIEvents.RaiseLevelSelectorReady();
    }

    private IEnumerator ShowLevelSelectorLegacyCo()
    {
        UIManager uiManager = UIManager.Instance;

        SetHUDActive(false);

        if (AudioService.Instance != null)
        {
            AudioService.Instance.StopAllSFX();
        }

        if (uiManager != null)
        {
            yield return uiManager.HideActivePanelsForSceneTransition(hideSplashScreen: true);
        }

        _transitionAnim.SetTrigger("OpeningStart");
        yield return new WaitForSecondsRealtime(_transitionTime);

        if (uiManager != null)
        {
            yield return uiManager.SetLevelsScreenVisibilityForTransition(true);
        }

        _transitionAnim.SetTrigger("End");
        AudioService.Instance?.PlaySFX(_audioContext.Audio.transitionSlash);

        UIEvents.RequestUpdateLivesUI(LifeManager.Instance?.CurrentLives ?? 0);

        EndSceneTransition();
        uiManager?.SetGameplayHUDEnabled(false);
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

        UIManager uiManager = UIManager.Instance;
        if (uiManager != null)
        {
            yield return uiManager.HideActivePanelsForSceneTransition();
        }

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

        if (uiManager != null)
        {
            yield return uiManager.SetSplashScreenVisibilityForTransition(false);
            yield return uiManager.SetLevelsScreenVisibilityForTransition(true);
        }

        UIEvents.RequestUpdateLivesUI(LifeManager.Instance?.CurrentLives ?? 0);

        _isLoadingLevelSelectorScene = false;
        EndSceneTransition();
        uiManager?.SetGameplayHUDEnabled(false);
        UIEvents.RaiseLevelSelectorReady();
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

    private bool TryBeginSceneTransition()
    {
        if (_isSceneTransitionInProgress)
            return false;

        _isSceneTransitionInProgress = true;
        UIManager.Instance?.SetUIRequestLock(true);
        return true;
    }

    private void EndSceneTransition()
    {
        _isSceneTransitionInProgress = false;
        UIManager.Instance?.SetUIRequestLock(false);
    }
}
