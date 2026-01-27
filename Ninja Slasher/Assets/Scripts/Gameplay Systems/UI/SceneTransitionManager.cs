using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("SCREEN TRANSITION")]
    [SerializeField] private Animator _transitionAnim;
    [SerializeField] private float _transitionTime;

    [SerializeField] private GameObject _hudObject;

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

        _transitionAnim.SetTrigger("Start");
        yield return new WaitForSeconds(_transitionTime);

        UIManager.Instance.SetLevelsScreenEnabled(false);

        SceneManager.LoadScene(sceneName);

        _transitionAnim.SetTrigger("End");
        AudioManager.Instance.PlaySFX(SFXClip.UI_TransitionSlash);

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

        UIManager.Instance.HideResultsModal();

        _transitionAnim.SetTrigger("OpeningStart");
        yield return new WaitForSeconds(_transitionTime);

        UIManager.Instance.HideSplashScreen();
        UIManager.Instance.SetLevelsScreenEnabled(true);
        UIManager.Instance.SetGameplayHUDEnabled(false);

        _transitionAnim.SetTrigger("End");
        AudioManager.Instance.PlaySFX(SFXClip.UI_TransitionSlash);

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