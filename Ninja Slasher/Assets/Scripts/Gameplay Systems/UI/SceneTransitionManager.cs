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

    private CanvasManager _canvasManager;

    private void Awake()
    {
        _canvasManager = GetComponent<CanvasManager>();
        _dailyRewardUI = GetComponentInChildren<DailyRewardUIManager>();
    }

    public void LoadLevelScene(string sceneName)
    {
        StartCoroutine(LoadLevelSceneCo(sceneName));
    }

    private IEnumerator LoadLevelSceneCo(string sceneName)
    {
        _transitionAnim.SetTrigger("Start");
        yield return new WaitForSeconds(_transitionTime);
        _canvasManager.SetLevelsCanvasEnabled(false);
        SceneManager.LoadScene(sceneName);
        _transitionAnim.SetTrigger("End");
        AudioManager.Instance.PlaySFX(SFXClip.UI_TransitionSlash);
        _canvasManager.SetGameplayCanvasEnabled(true);
    }

    public void RestartLevel()
    {
        //if (!LifeManager.Instance.CanPlay())
        //{
        //    GetComponent<GameplayUIManager>().ShowNoLivesPanel();
        //    return;
        //}
        string sceneName = SceneManager.GetActiveScene().name;
        LoadLevelScene(sceneName);
        UIManager.Instance.ShowHidePauseCanvas();
    }

    public void ShowLevelSelector()
    {
        StartCoroutine(ShowLevelSelectorCo());
        GetComponent<GameplayUIManager>().UpdateLivesUI(LifeManager.Instance.CurrentLives);
        GetComponent<DebugUIManager>()?.ShowStarsDebug();
        CheckAndShowDailyRewards();
    }

    private IEnumerator ShowLevelSelectorCo()
    {
        Time.timeScale = 1;
        _transitionAnim.SetTrigger("OpeningStart");
        yield return new WaitForSeconds(_transitionTime);
        _canvasManager.SetSplashCanvasEnabled(false);
        _canvasManager.SetLevelsCanvasEnabled(true);
        _canvasManager.SetPauseCanvasEnabled(false);
        _transitionAnim.SetTrigger("End");
        AudioManager.Instance.PlaySFX(SFXClip.UI_TransitionSlash);
    }

    private void CheckAndShowDailyRewards()
    {
        if (DailyRewardSystem.Instance == null)
        {
            return;
        }

        bool canClaim = DailyRewardSystem.Instance.CanClaimToday();
        Debug.Log($"[UIManager] CanClaimToday: {canClaim}");

        if (canClaim)
        {
            StartCoroutine(ShowDailyRewardsAfterDelay(1f));
        }
    }

    private IEnumerator ShowDailyRewardsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_dailyRewardUI == null)
        {
            yield break;
        }

        _canvasManager.ShowHideDailyRewardCanvas();

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
        _canvasManager.SetGameplayCanvasEnabled(true);
    }
}