using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviourSingleton<UIManager>
{
    [Header("TRANSITION ANIM")]
    [SerializeField] private Animator _transitionAnim;
    [SerializeField] private float _transitionTime;

    [Header("CANVAS")]
    [SerializeField] private GameObject _splashCanvas;
    [SerializeField] private GameObject _levelsCanvas;
    [SerializeField] private GameObject _gameplayCanvas;
    [SerializeField] private GameObject _pauseCanvas;


    public void LoadNextLevelScene()
    {
        StartCoroutine(LoadNextLevelSceneCo(SceneManager.GetActiveScene().buildIndex + 1));
    }

    IEnumerator LoadNextLevelSceneCo(int levelIndex)
    {
        _transitionAnim.SetTrigger("Start");
        yield return new WaitForSeconds(_transitionTime);
        _levelsCanvas.SetActive(false);
        SceneManager.LoadScene(levelIndex);
        _transitionAnim.SetTrigger("End");
        _gameplayCanvas.SetActive(true);
    }
    public void ShowLevelSelector()
    {
        StartCoroutine(ShowLevelSelectorCo());
    }
    IEnumerator ShowLevelSelectorCo()
    {
        _transitionAnim.SetTrigger("Start");
        yield return new WaitForSeconds(_transitionTime);
        _splashCanvas.SetActive(false);
        _levelsCanvas.SetActive(true);
        _transitionAnim.SetTrigger("End");
    }
    
}
