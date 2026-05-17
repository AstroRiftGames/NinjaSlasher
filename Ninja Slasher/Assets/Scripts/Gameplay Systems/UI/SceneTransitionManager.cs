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

    public bool ShouldRunLevelSelectionStartupFlowOnNextEntry =>
        _dailyStartupSequence != null && _dailyStartupSequence.ShouldRunOnNextLevelSelectorReady;

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

        UIEvents.RaiseTransitionFinished();

        EndTransitionPause();
        EndSceneTransition();
        uiManager?.RefreshGameplayHUDSessionVisibility();

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

        yield return PlayLegacyTransitionAndWait("Start");

        SceneManager.LoadScene(sceneName);
        yield return WaitForScenePresentationFrame();

        yield return PlayLegacyTransitionAndWait("End", playSlashSfx: true);

        UIEvents.RaiseTransitionFinished();

        EndTransitionPause();
        EndSceneTransition();
        uiManager?.RefreshGameplayHUDSessionVisibility();

        yield return new WaitForEndOfFrame();
        SetHUDActive(true);
    }

    public void LoadLevelSelectorScene()
    {
        if (_isLoadingLevelSelectorScene || !TryBeginSceneTransition())
            return;

        ConfigureNextLevelSelectorEntry(shouldRunStartupSequence: false);

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

        yield return PlayLegacyTransitionAndWait("OpeningStart");

        SceneManager.sceneLoaded += OnLevelSelectorSceneLoaded;
        SceneManager.LoadScene(_levelSelectorSceneName);
    }

    public void ShowLevelSelector()
    {
        if (!TryBeginSceneTransition())
            return;

        ConfigureNextLevelSelectorEntry(shouldRunStartupSequence: true);

        if (HasKatanaTransition())
        {
            StartCoroutine(ShowLevelSelectorWithKatanaCo());
            return;
        }

        StartCoroutine(ShowLevelSelectorLegacyCo());
    }

    private void ConfigureNextLevelSelectorEntry(bool shouldRunStartupSequence)
    {
        _dailyStartupSequence?.ConfigureNextLevelSelectorEntry(shouldRunStartupSequence);
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

        yield return PlayLegacyTransitionAndWait("OpeningStart");

        if (uiManager != null)
        {
            yield return uiManager.SetLevelsScreenVisibilityForTransition(true);
        }

        yield return PlayLegacyTransitionAndWait("End", playSlashSfx: true);

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
            if (uiManager != null)
            {
                yield return uiManager.SetSplashScreenVisibilityForTransition(false);
                yield return uiManager.SetLevelsScreenVisibilityForTransition(true);
            }

            _katanaTransition.PlayExitLevelTransition();
            yield return WaitForKatanaTransitionToComplete();
        }
        else
        {
            if (uiManager != null)
            {
                yield return uiManager.SetSplashScreenVisibilityForTransition(false);
                yield return uiManager.SetLevelsScreenVisibilityForTransition(true);
            }

            yield return PlayLegacyTransitionAndWait("End", playSlashSfx: true);
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

    private IEnumerator PlayLegacyTransitionAndWait(string triggerName, bool playSlashSfx = false)
    {
        if (_transitionAnim != null && !string.IsNullOrEmpty(triggerName))
            _transitionAnim.SetTrigger(triggerName);

        if (playSlashSfx)
            AudioService.Instance?.PlaySFX(_audioContext.Audio.transitionSlash);

        yield return WaitForLegacyTransitionToComplete();
    }

    private IEnumerator WaitForLegacyTransitionToComplete()
    {
        if (_transitionAnim == null)
        {
            yield return WaitForSecondsUnscaled(_transitionTime);
            yield break;
        }

        yield return null;

        if (_transitionAnim == null || !_transitionAnim.isActiveAndEnabled || !_transitionAnim.gameObject.activeInHierarchy)
        {
            yield return WaitForSecondsUnscaled(_transitionTime);
            yield break;
        }

        float timeout = Mathf.Max(0.1f, _transitionTime + 0.5f);
        float elapsed = 0f;
        int initialStateHash = _transitionAnim.GetCurrentAnimatorStateInfo(0).fullPathHash;
        bool observedPlayback = false;

        while (elapsed < timeout)
        {
            if (_transitionAnim == null || !_transitionAnim.isActiveAndEnabled || !_transitionAnim.gameObject.activeInHierarchy)
            {
                yield return WaitForSecondsUnscaled(Mathf.Max(0f, _transitionTime - elapsed));
                yield break;
            }

            AnimatorStateInfo state = _transitionAnim.GetCurrentAnimatorStateInfo(0);
            if (_transitionAnim.IsInTransition(0) || state.fullPathHash != initialStateHash)
            {
                observedPlayback = true;
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!observedPlayback)
        {
            yield return WaitForSecondsUnscaled(_transitionTime);
            yield break;
        }

        elapsed = 0f;
        while (elapsed < timeout)
        {
            if (_transitionAnim == null || !_transitionAnim.isActiveAndEnabled || !_transitionAnim.gameObject.activeInHierarchy)
                yield break;

            AnimatorStateInfo state = _transitionAnim.GetCurrentAnimatorStateInfo(0);
            if (!_transitionAnim.IsInTransition(0) && state.normalizedTime >= 1f)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static IEnumerator WaitForScenePresentationFrame()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
    }

    private static IEnumerator WaitForSecondsUnscaled(float duration)
    {
        if (duration <= 0f)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
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
