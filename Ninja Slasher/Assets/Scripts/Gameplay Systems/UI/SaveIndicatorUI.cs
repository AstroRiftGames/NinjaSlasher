using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveIndicatorUI : MonoBehaviourSingleton<SaveIndicatorUI>
{
    [Header("UI Elements")]
    [SerializeField] private GameObject saveIndicatorPanel;
    [SerializeField] private Image saveIcon;
    [SerializeField] private TextMeshProUGUI saveText;

    [Header("Animation Settings")]
    [SerializeField] private float showDuration = 1.5f;
    [SerializeField] private float fadeInTime = 0.3f;
    [SerializeField] private float fadeOutTime = 0.5f;

    private CanvasGroup canvasGroup;
    private Coroutine showCoroutine;
    private bool isShowing = false;

    public override void Awake()
    {
        base.Awake();
        InitializeUI();
    }

    void InitializeUI()
    {
        canvasGroup = saveIndicatorPanel.GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        saveIndicatorPanel.SetActive(false);
    }

    public void ShowSaveIndicator(string message = "Guardando...")
    {
        if (isShowing) return;

        if (saveText != null)
            saveText.text = message;

        if (showCoroutine != null)
            StopCoroutine(showCoroutine);

        showCoroutine = StartCoroutine(ShowIndicatorCoroutine());
    }

    public void ShowSaveIndicator(float duration, string message = "Guardando...")
    {
        showDuration = duration;
        ShowSaveIndicator(message);
    }

    public void HideSaveIndicator()
    {
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }

        StartCoroutine(FadeOut());
    }

    IEnumerator ShowIndicatorCoroutine()
    {
        isShowing = true;
        saveIndicatorPanel.SetActive(true);

        yield return StartCoroutine(FadeIn());

        yield return new WaitForSeconds(showDuration);

        yield return StartCoroutine(FadeOut());

        isShowing = false;
    }

    IEnumerator FadeIn()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInTime);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutTime);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        saveIndicatorPanel.SetActive(false);
    }

#if UNITY_EDITOR
    [ContextMenu("Test Save Indicator")]
    void TestSaveIndicator()
    {
        ShowSaveIndicator("Prueba de guardado...");
    }
#endif
}