using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelsScreen : UIScreenBase
{
    [SerializeField] private float _delayBeforeAnimation = 0.3f;
    [SerializeField] private GameObject _infoRoot;
    [SerializeField] private GameObject _buttonsRoot;
    [SerializeField] private AreaSectionController[] _areaSections;

    private bool _hasPlayedIntroAnimation = false;
    private bool _isWaitingForStartupSequence = false;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        UIEvents.OnStartupSequenceCompleted += OnStartupSequenceCompleted;

        if (_areaSections == null) return;
        foreach (var area in _areaSections)
            if (area != null) area.OnUnlocked += OnAreaUnlocked;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
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
        }

        if (!_hasPlayedIntroAnimation)
        {
            _isWaitingForStartupSequence = true;
            SetStartupSequenceVisualsVisible(false);
            HideLevelButtons();
        }
        else
        {
            _isWaitingForStartupSequence = false;
            SetStartupSequenceVisualsVisible(true);
            ShowLevelButtonsInstantly();
        }

        NotifyPanelShown();
        OnShown();
    }

    public override void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;

        SetPanelInputEnabled(false);

        ButtonManager.Instance?.StopAllButtonAnimations();

        OnHidden();

        gameObject.SetActive(false);
    }

    private void OnStartupSequenceCompleted()
    {
        if (!_isWaitingForStartupSequence || !isActiveAndEnabled)
            return;

        _isWaitingForStartupSequence = false;
        _hasPlayedIntroAnimation = true;
        SetStartupSequenceVisualsVisible(true);
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

    private void ShowLevelButtonsInstantly()
    {
        if (ButtonManager.Instance == null) return;

        ButtonManager.Instance.StopAllButtonAnimations();
        ButtonManager.Instance.ShowButtonsInstantly(ButtonManager.Instance.GetLevelButtons());
    }

    public void ResetAnimationStateForScreenReturn()
    {
        _isWaitingForStartupSequence = false;
        SetStartupSequenceVisualsVisible(true);
        ShowLevelButtonsInstantly();
    }

    private void SetStartupSequenceVisualsVisible(bool visible)
    {
        if (_infoRoot != null)
            _infoRoot.SetActive(visible);

        if (_buttonsRoot != null)
            _buttonsRoot.SetActive(visible);
    }
}
