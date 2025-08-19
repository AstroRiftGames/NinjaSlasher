using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadManager : MonoBehaviour
{
    [SerializeField] private Slider _loadbar;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Animator _anim;

    private void Start()
    {
        SceneLoad(SceneManager.GetActiveScene().buildIndex);
        _text.text = "LOADING...";
    }
    public void SceneLoad(int sceneIndex)
    {
        StartCoroutine(LoadAsync(sceneIndex));
    }

    IEnumerator LoadAsync(int sceneIndex)
    {
        yield return new WaitForSeconds(5f);
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneIndex);
        asyncOperation.allowSceneActivation = false;

        while (!asyncOperation.isDone)
        {
            _loadbar.value += Mathf.Lerp(0f, 1f, 0.2f) * Time.deltaTime;
            if (_loadbar.value >= 1)
            {
                _text.text = "TAP TO CONTINUE";
                _anim.SetTrigger("Tap");

                bool tapped = false;

#if UNITY_EDITOR
                if (Input.anyKeyDown) tapped = true;
#endif
                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) tapped = true;

                if (tapped)
                {
                    yield return StartCoroutine(WaitAuthAndInitialCloudSync());

                    UIManager.Instance.ShowLevelSelector();
                    yield return new WaitForSeconds(2);
                    asyncOperation.allowSceneActivation = true;
                }
            }
            yield return null;
        }
    }

    private IEnumerator WaitAuthAndInitialCloudSync()
    {
        float timeout = 8f, t = 0f;
        while (!AuthManager.Instance || !AuthManager.Instance.IsSignedIn())
        {
            if (t >= timeout) break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (CloudSaveManager.Instance)
        {
            bool available = false;
            float t2 = 0f;
            while (!(available = CloudSaveManager.Instance.IsCloudSaveAvailable) && t2 < 8f)
            {
                t2 += Time.unscaledDeltaTime;
                yield return null;
            }

            if (available)
            {
                var task = CloudSaveManager.Instance.EnsureInitialSync();
                while (!task.IsCompleted) yield return null;
            }
        }
    }
}
