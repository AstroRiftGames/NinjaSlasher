using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class LoadManager : MonoBehaviour
{
    [SerializeField] private Slider _loadbar;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Animator _textAnim;
    [SerializeField] private RectMask2D _rectMask2D;

    [SerializeField] private float _loadingDuration = 3f;
    [SerializeField] private Ease _loadingEase = Ease.OutQuart;

    private UIAudioContext _audioContext;
    private bool _continueRequested;

    private void Awake()
    {
        _audioContext = GetComponentInParent<UIAudioContext>();
    }

    private void Start()
    {
        SceneLoad(SceneManager.GetActiveScene().buildIndex);
        _text.text = "CARGANDO...";
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

        _loadbar.value = 0f;
        _loadbar.DOValue(1f, _loadingDuration).SetEase(_loadingEase);

        while (!asyncOperation.isDone)
        {
            _rectMask2D.padding = new Vector4(_loadbar.value*1400f, 0, 0, 0);
            

            if (_loadbar.value >= 1)
            {
                _text.text = "PRESIONA PARA CONTINUAR";
                _textAnim.SetTrigger("Tap");

                if (Input.touchCount > 0)
                {
                    Touch touch = Input.GetTouch(0);
                    if (touch.phase == TouchPhase.Began)
                    {
                        if (_continueRequested)
                            continue;

                        _continueRequested = true;
                        UIEvents.RequestShowLevelSelector();
                        yield return new WaitForSeconds(2);

                        asyncOperation.allowSceneActivation = true;
                    }
                }
#if UNITY_EDITOR
                if (Input.anyKeyDown)
                {
                    if (_continueRequested)
                        continue;

                    _continueRequested = true;
                    UIEvents.RequestShowLevelSelector();

                    yield return new WaitForSeconds(2);

                    asyncOperation.allowSceneActivation = true;
                }
#endif
            }
            yield return null;
        }
    }
}
