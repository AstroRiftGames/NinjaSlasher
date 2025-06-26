using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadManager : MonoBehaviour
{
    [SerializeField] private Slider _loadbar;

    private void Start()
    {
        SceneLoad(SceneManager.GetActiveScene().buildIndex);
    }
    public void SceneLoad(int sceneIndex)
    {
        StartCoroutine(LoadAsync(sceneIndex));
    }

    IEnumerator LoadAsync(int sceneIndex)
    {
        yield return new WaitForSeconds(2f);
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneIndex);
        asyncOperation.allowSceneActivation = false;

        while (!asyncOperation.isDone)
        {
            _loadbar.value = asyncOperation.progress/0.9f;
            if (asyncOperation.progress >= 0.89)
            {
                UIManager.Instance.ShowLevelSelector();
                yield return new WaitForSeconds(2);
                asyncOperation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
