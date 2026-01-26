using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("SCREEN TRANSITION")]
    [SerializeField] private Animator _transitionAnim;
    [SerializeField] private float _transitionTime;

    [Header("DAILY REWARDS")]
    [SerializeField] private DailyRewardUIManager _dailyRewardUI;

    [Header("DAILY WHEEL")]
    [SerializeField] private DailyWheelUI _dailyWheelUI;

    [SerializeField] private GameObject _hudObject;

    private CanvasManager _canvasManager;

    private void Awake()
    {
        _canvasManager = GetComponent<CanvasManager>();
        _dailyRewardUI = GetComponentInChildren<DailyRewardUIManager>();
        _dailyWheelUI = GetComponentInChildren<DailyWheelUI>();
    }

    private void OnEnable()
    {
        if (_dailyWheelUI != null)
        {
            _dailyWheelUI.OnWheelProcessComplete += OnWheelCompleted;
        }
    }

    private void OnDisable()
    {
        if (_dailyWheelUI != null)
        {
            _dailyWheelUI.OnWheelProcessComplete -= OnWheelCompleted;
        }
    }

    public void LoadLevelScene(string sceneName)
    {
        StartCoroutine(LoadLevelSceneCo(sceneName));
    }

    private IEnumerator LoadLevelSceneCo(string sceneName)
    {
        SetHUDActive(false);

        _transitionAnim.SetTrigger("Start");
        yield return new WaitForSeconds(_transitionTime);
        _canvasManager.SetLevelsCanvasEnabled(false);
        SceneManager.LoadScene(sceneName);
        _transitionAnim.SetTrigger("End");
        AudioManager.Instance.PlaySFX(SFXClip.UI_TransitionSlash);
        //_canvasManager.SetGameplayCanvasEnabled(true);
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
        GetComponent<GameplayUIManager>().UpdateLivesUI(LifeManager.Instance.CurrentLives);
        GetComponent<DebugUIManager>()?.ShowStarsDebug();
        CheckAndShowDailySequence();
    }

    private IEnumerator ShowLevelSelectorCo()
    {
        Time.timeScale = 1;

        SetHUDActive(false);

        _canvasManager.CloseCanvas(_canvasManager.GetResultsCanvas());

        _transitionAnim.SetTrigger("OpeningStart");
        yield return new WaitForSeconds(_transitionTime);
        //_canvasManager.SetSplashCanvasEnabled(false);
        UIManager.Instance.HideSplashScreen();
        _canvasManager.SetLevelsCanvasEnabled(true);
        //_canvasManager.SetGameplayCanvasEnabled(false);
        UIManager.Instance.SetGameplayHUDEnabled(true);
        _transitionAnim.SetTrigger("End");
        AudioManager.Instance.PlaySFX(SFXClip.UI_TransitionSlash);
    }

    private void CheckAndShowDailySequence()
    {
        StartCoroutine(CheckAndShowDailySequenceWhenReady());
    }

    private IEnumerator CheckAndShowDailySequenceWhenReady()
    {
        while (SaveManager.Instance == null || !SaveManager.Instance.IsDataLoaded ||
               DailyWheelSystem.Instance == null || DailyRewardSystem.Instance == null)
        {
            yield return null;
        }

        yield return null;

        bool canSpinWheel = DailyWheelSystem.Instance.CanSpinToday();
        bool canClaimReward = DailyRewardSystem.Instance.CanClaimToday();

        if (canSpinWheel)
        {
            StartCoroutine(ShowDailyWheelAfterDelay(1f));
        }
        else if (canClaimReward)
        {
            StartCoroutine(ShowDailyRewardsAfterDelay(1f));
        }
    }

    private IEnumerator ShowDailyWheelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_dailyWheelUI == null)
        {
            Debug.LogWarning("[SceneTransition] DailyWheelUI no encontrado");
            CheckAndShowDailyRewardsAfterWheel();
            yield break;
        }

        _canvasManager.ShowHideDailyWheelCanvas();
    }

    private void OnWheelCompleted()
    {
        _canvasManager.ShowHideDailyWheelCanvas();

        CheckAndShowDailyRewardsAfterWheel();
    }

    private void CheckAndShowDailyRewardsAfterWheel()
    {
        StartCoroutine(CheckAndShowDailyRewardsAfterWheelCo());
    }

    private IEnumerator CheckAndShowDailyRewardsAfterWheelCo()
    {
        yield return new WaitForSeconds(0.5f);

        bool canClaimReward = DailyRewardSystem.Instance.CanClaimToday();
        Debug.Log($"[SceneTransition] CanClaimReward (after wheel): {canClaimReward}");

        if (canClaimReward)
        {
            StartCoroutine(ShowDailyRewardsAfterDelay(0.5f));
        }
    }

    private IEnumerator ShowDailyRewardsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_dailyRewardUI == null)
        {
            Debug.LogWarning("[SceneTransition] DailyRewardUI no encontrado");
            yield break;
        }

        UIManager.Instance.ShowHideDailyRewardCanvas();

        _dailyRewardUI.ShowDailyReward();
    }

    public void LoadDebugTestScene()
    {
        StartCoroutine(LoadDebugTestLevel());
    }

    private IEnumerator LoadDebugTestLevel()
    {
        _transitionAnim.SetTrigger("Start");
        yield return new WaitForSeconds(_transitionTime);
        _canvasManager.SetLevelsCanvasEnabled(false);
        SceneManager.LoadScene("TestScene");
        _transitionAnim.SetTrigger("End");
        //_canvasManager.SetGameplayCanvasEnabled(true);
        UIManager.Instance.SetGameplayHUDEnabled(true);
    }

    private void SetHUDActive(bool active)
    {
        if (_hudObject != null)
        {
            _hudObject.SetActive(active);
        }
    }
}