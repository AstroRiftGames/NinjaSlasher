using UnityEngine;
using System.Collections;

public class LevelsScreen : UIScreenBase
{
    [Header("Animation")]
    [SerializeField] private float _delayBeforeAnimation = 0.3f;

    private bool _hasAnimatedButtons = false;

    protected override void Awake()
    {
        base.Awake();
    }

    public override void Show()
    {
        if (_isVisible) return;

        gameObject.SetActive(true);
        _isVisible = true;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        if (!_hasAnimatedButtons)
        {
            _hasAnimatedButtons = true;
            StartCoroutine(AnimateLevelButtonsSequence());
        }

        OnShown();
    }

    public override void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        OnHidden();

        gameObject.SetActive(false);
    }

    private IEnumerator AnimateLevelButtonsSequence()
    {
        // Ocultar botones primero
        if (ButtonManager.Instance != null)
        {
            HideLevelButtons();
        }

        yield return null;
        yield return new WaitForSeconds(_delayBeforeAnimation);

        if (UIManager.Instance.IsDailyRewardModalVisible())
        {
            while (UIManager.Instance.IsDailyRewardModalVisible())
            {
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(0.5f);
        }

        if (ButtonManager.Instance != null)
        {
            ButtonManager.Instance.TriggerNinjaWaveAnimation();
        }
    }

    private void HideLevelButtons()
    {
        if (ButtonManager.Instance == null) return;

        var levelButtons = ButtonManager.Instance.GetLevelButtons();
        if (levelButtons == null) return;

        foreach (var button in levelButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }
    }

    public void ResetAnimationFlag()
    {
        _hasAnimatedButtons = false;
    }

    protected override void OnShown()
    {
        Debug.Log("[LevelsScreen] Screen de selección de niveles mostrado");
    }

    protected override void OnHidden()
    {
        Debug.Log("[LevelsScreen] Screen de selección de niveles ocultado");
    }
}