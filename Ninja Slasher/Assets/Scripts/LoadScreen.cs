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
                if (Input.touchCount > 0)
                {
                    Touch touch = Input.GetTouch(0);

                    if (touch.phase == TouchPhase.Began)
                    {
                        AudioManager.Instance.PlaySFX(SFXClip.UI_TapSplashScreen);
                        UIManager.Instance.ShowLevelSelector();
                        yield return new WaitForSeconds(2);
                        AudioManager.Instance.PlaySFX(SFXClip.UI_TransitionSlash);
                        asyncOperation.allowSceneActivation = true;
                    }
                }

#if UNITY_EDITOR
                if (Input.anyKeyDown)
                {
                    AudioManager.Instance.PlaySFX(SFXClip.UI_TapSplashScreen);
                    UIManager.Instance.ShowLevelSelector();
                    yield return new WaitForSeconds(2);
                    AudioManager.Instance.PlayMusic(MusicClip.MainMenu, true);
                    AudioManager.Instance.PlaySFX(SFXClip.UI_TransitionSlash);
                    asyncOperation.allowSceneActivation = true;
                }
#endif
            }

            yield return null;
        }
    }
}
