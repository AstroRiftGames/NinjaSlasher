using System.Collections;
using UnityEngine;
using DG.Tweening;

public class CanvasManager : MonoBehaviour
{
    [Header("CANVAS")]
    [SerializeField] private Canvas _levelsCanvas;

    [Header("ANIMATION")]
    [SerializeField] private float _animationDuration = 0.1f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InBack;
    [SerializeField] private float _scaleOvershoot = 1.05f;

    private bool _hasAnimatedButtons = false;

    public void OpenCanvas(Canvas canvas) => canvas.enabled = true;
    public void CloseCanvas(Canvas canvas) => canvas.enabled = false;

    public void ShowHideCanvas(Canvas canvas, bool state)
    {
        AudioManager.Instance.PlaySFX(SFXClip.UI_Select);

        if (state)
            ShowCanvasAnimated(canvas);
        else
            HideCanvasAnimated(canvas);
    }

    private void ShowCanvasAnimated(Canvas canvas)
    {
        canvas.enabled = true;

        RectTransform panelToAnimate = GetPanelForCanvas(canvas);
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(canvas);

        if (panelToAnimate != null)
        {
            ShowPanelAnimated(panelToAnimate);
        }
        else
        {
            ShowCanvasGroupAnimated(canvasGroup);
        }
    }

    private void HideCanvasAnimated(Canvas canvas)
    {
        RectTransform panelToAnimate = GetPanelForCanvas(canvas);
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(canvas);

        if (panelToAnimate != null)
        {
            HidePanelAnimated(panelToAnimate, canvas);
        }
        else
        {
            HideCanvasGroupAnimated(canvasGroup, canvas);
        }
    }

    private void ShowPanelAnimated(RectTransform panel)
    {
        DOTween.Kill(panel);

        panel.localScale = Vector3.zero;

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        Sequence showSequence = DOTween.Sequence();
        showSequence.Append(panel.DOScale(_scaleOvershoot, _animationDuration * 0.7f)
            .SetEase(_openEase));
        showSequence.Append(panel.DOScale(1f, _animationDuration * 0.3f)
            .SetEase(Ease.InOutQuad));

        showSequence.SetUpdate(true);
    }

    private void HidePanelAnimated(RectTransform panel, Canvas canvas)
    {
        DOTween.Kill(panel);

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            DOTween.Kill(canvasGroup);
        }

        panel.DOScale(0f, _animationDuration)
            .SetEase(_closeEase)
            .OnComplete(() =>
            {
                canvas.enabled = false;

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }

                panel.localScale = Vector3.one;
            })

            .SetUpdate(true);
    }

    private void ShowCanvasGroupAnimated(CanvasGroup canvasGroup)
    {
        DOTween.Kill(canvasGroup);

        canvasGroup.alpha = 0f;
        canvasGroup.transform.localScale = Vector3.zero;

        Sequence showSequence = DOTween.Sequence();
        showSequence.Append(canvasGroup.DOFade(1f, _animationDuration * 0.6f));
        showSequence.Join(canvasGroup.transform.DOScale(_scaleOvershoot, _animationDuration * 0.7f)
            .SetEase(_openEase));
        showSequence.Append(canvasGroup.transform.DOScale(1f, _animationDuration * 0.3f)
            .SetEase(Ease.InOutQuad));

        showSequence.SetUpdate(true);
    }

    private void HideCanvasGroupAnimated(CanvasGroup canvasGroup, Canvas canvas)
    {
        DOTween.Kill(canvasGroup);

        Sequence hideSequence = DOTween.Sequence();
        hideSequence.Append(canvasGroup.transform.DOScale(0f, _animationDuration)
            .SetEase(_closeEase));
        hideSequence.Join(canvasGroup.DOFade(0f, _animationDuration * 0.8f));
        hideSequence.OnComplete(() => canvas.enabled = false);

        hideSequence.SetUpdate(true);
    }

    private RectTransform GetPanelForCanvas(Canvas canvas)
    {
        return null;
    }

    private CanvasGroup GetOrAddCanvasGroup(Canvas canvas)
    {
        CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        return canvasGroup;
    }

    public void SetLevelsCanvasEnabled(bool enabled)
    {
        _levelsCanvas.enabled = enabled;

        if (enabled && !_hasAnimatedButtons)
        {
            _hasAnimatedButtons = true;
            ButtonManager buttonManager = GetComponent<ButtonManager>();
            if (buttonManager != null)
            {
                HideLevelButtons(buttonManager);
            }
            StartCoroutine(TriggerButtonAnimation());
        }
    }

    private IEnumerator TriggerButtonAnimation()
    {
        yield return null;
        yield return new WaitForSeconds(0.3f);

        if (UIManager.Instance.IsDailyRewardModalVisible())
        {
            while (UIManager.Instance.IsDailyRewardModalVisible())
            {
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(0.5f);
        }

        ButtonManager buttonManager = GetComponent<ButtonManager>();
        if (buttonManager != null)
        {
            buttonManager.TriggerNinjaWaveAnimation();
        }
    }

    private void HideLevelButtons(ButtonManager buttonManager)
    {
        var levelButtons = buttonManager.GetLevelButtons();
        foreach (var button in levelButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }
    }

    private void ToggleCanvas(Canvas canvas)
    {
        bool isCanvasActive = !canvas.enabled;
        ShowHideCanvas(canvas, isCanvasActive);
    }

    public Canvas GetResultsCanvas() => null;

    private void OnDisable()
    {
        DOTween.KillAll();
    }
}