using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelsScreen : UIScreenBase
{
    [SerializeField] private float _delayBeforeAnimation = 0.3f;
    [SerializeField] private AreaSectionController[] _areaSections;

    private bool _hasAnimatedButtons = false;
    private bool _isWaitingForStartupSequence = false;

    protected override void Awake()
    {
        base.Awake();
    }

    private void OnEnable()
    {
        UIEvents.OnStartupSequenceCompleted += OnStartupSequenceCompleted;

        if (_areaSections == null) return;
        foreach (var area in _areaSections)
            if (area != null) area.OnUnlocked += OnAreaUnlocked;
    }

    private void OnDisable()
    {
        UIEvents.OnStartupSequenceCompleted -= OnStartupSequenceCompleted;

        if (_areaSections == null) return;
        foreach (var area in _areaSections)
            if (area != null) area.OnUnlocked -= OnAreaUnlocked;
    }

    private void OnAreaUnlocked(AreaSectionController area)
    {
        var buttons = area.GetAreaButtons();
        if (buttons.Length > 0)
            ButtonManager.Instance?.AnimateButtons(buttons);
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
            _isWaitingForStartupSequence = true;
            HideLevelButtons();
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

        ButtonManager.Instance?.StopAllButtonAnimations();

        OnHidden();

        gameObject.SetActive(false);
    }

    private void OnStartupSequenceCompleted()
    {
        if (!_isWaitingForStartupSequence || !isActiveAndEnabled)
            return;

        _isWaitingForStartupSequence = false;
        StartCoroutine(AnimateLevelButtonsSequence());
    }

    private IEnumerator AnimateLevelButtonsSequence()
    {
        yield return null;
        yield return new WaitForSeconds(_delayBeforeAnimation);
        yield return new WaitForSeconds(0.5f);

        if (ButtonManager.Instance == null) yield break;

        var visibleButtons = CollectUnlockedAreaButtons();
        ButtonManager.Instance.AnimateButtons(visibleButtons);
    }

    private List<Button> CollectUnlockedAreaButtons()
    {
        var result = new List<Button>();

        if (_areaSections == null || _areaSections.Length == 0)
        {
            return result;
        }

        foreach (var area in _areaSections)
        {
            if (area == null) continue;
            if (!area.IsUnlocked()) continue;

            result.AddRange(area.GetAreaButtons());
        }

        return result;
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
        _isWaitingForStartupSequence = false;
    }
}
