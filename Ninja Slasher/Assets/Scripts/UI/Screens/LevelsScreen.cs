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
    [SerializeField] private LevelSelectionScreenController _presenter;
    [SerializeField] private ButtonManager _buttonManager;

    private bool _hasPlayedIntroAnimation = false;
    private bool _isWaitingForStartupSequence = false;
    private bool _isBlockedByForegroundSignal = false;
    private bool _isForegroundVisible = true;

    protected override void Awake()
    {
        base.Awake();
        ResolveDependencies();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ResolveDependencies();
        UIEvents.OnStartupSequenceStarted += OnStartupSequenceStarted;
        UIEvents.OnStartupSequenceCompleted += OnStartupSequenceCompleted;
        UIPanel.OnBlockingPanelVisibilityChanged += OnBlockingPanelVisibilityChanged;
        SyncExternalForegroundSignals("OnEnable");

        if (_areaSections == null) return;
        foreach (var area in _areaSections)
            if (area != null) area.OnUnlocked += OnAreaUnlocked;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        UIEvents.OnStartupSequenceStarted -= OnStartupSequenceStarted;
        UIEvents.OnStartupSequenceCompleted -= OnStartupSequenceCompleted;
        UIPanel.OnBlockingPanelVisibilityChanged -= OnBlockingPanelVisibilityChanged;

        if (_areaSections == null) return;
        foreach (var area in _areaSections)
            if (area != null) area.OnUnlocked -= OnAreaUnlocked;
    }

    private void OnAreaUnlocked(AreaSectionController area)
    {
        var buttons = area.GetAreaButtons();
        if (buttons.Length == 0)
            return;

        if (_buttonManager == null)
        {
            Debug.LogWarning("[LevelsScreen] ButtonManager was not found. Area unlock reveal was skipped.");
            return;
        }

        Debug.Log($"[LevelsScreen] Reveal -> AreaUnlocked | Buttons={buttons.Length}");
        _buttonManager.AnimateLevelButtonsReveal(buttons, "AreaUnlocked");
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
            EnterStartupSuppressedState("Show/FirstEntry");
            HideLevelButtons();
        }
        else
        {
            ExitStartupSuppressedState("Show/ReturnEntry");
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

        _buttonManager?.StopAllButtonAnimations();

        OnHidden();

        gameObject.SetActive(false);
    }

    private void OnStartupSequenceCompleted()
    {
        if (!_isWaitingForStartupSequence || !isActiveAndEnabled)
            return;

        _hasPlayedIntroAnimation = true;
        ExitStartupSuppressedState("StartupSequenceCompleted");
        StartCoroutine(AnimateLevelButtonsSequence());
    }

    private void OnStartupSequenceStarted()
    {
        if (!isActiveAndEnabled)
            return;

        EnterStartupSuppressedState("StartupSequenceStartedSignal");
    }

    private void OnBlockingPanelVisibilityChanged()
    {
        if (!isActiveAndEnabled)
            return;

        ApplyBlockingPanelSignal(IsBlockedByHigherPanel, "UIPanel.OnBlockingPanelVisibilityChanged");
    }

    private IEnumerator AnimateLevelButtonsSequence()
    {
        yield return null;

        if (_delayBeforeAnimation > 0f)
            yield return new WaitForSeconds(_delayBeforeAnimation);

        if (_buttonManager == null)
        {
            Debug.LogWarning("[LevelsScreen] ButtonManager was not found. Intro reveal animation was skipped.");
            yield break;
        }

        var visibleButtons = CollectUnlockedAreaButtons();
        Debug.Log($"[LevelsScreen] Reveal -> IntroSequence | Buttons={visibleButtons.Count}");
        _buttonManager.AnimateLevelButtonsReveal(visibleButtons, "IntroSequence");
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
        if (_buttonManager == null)
        {
            Debug.LogWarning("[LevelsScreen] ButtonManager was not found. HideLevelButtons was skipped.");
            return;
        }

        _buttonManager.HideAllLevelButtons();
    }

    private void ShowLevelButtonsInstantly()
    {
        if (_buttonManager == null)
        {
            Debug.LogWarning("[LevelsScreen] ButtonManager was not found. ShowLevelButtonsInstantly was skipped.");
            return;
        }

        _buttonManager.StopAllButtonAnimations();
        _buttonManager.ShowAllLevelButtonsInstantly();
    }

    public void ResetAnimationStateForScreenReturn()
    {
        ExitStartupSuppressedState("ResetAnimationStateForScreenReturn");
        ShowLevelButtonsInstantly();
    }

    public void EnterStartupSuppressedState(string reason = null)
    {
        ApplyStartupSequenceSignal(true, reason ?? "EnterStartupSuppressedState");
    }

    public void ExitStartupSuppressedState(string reason = null)
    {
        ApplyStartupSequenceSignal(false, reason ?? "ExitStartupSuppressedState");
    }

    private void RefreshForegroundVisibility(string reason = null)
    {
        bool shouldShow = ShouldForegroundBeVisible();
        SetForegroundVisible(shouldShow, reason ?? "RefreshForegroundVisibility");
    }

    public void SetForegroundVisible(bool visible, string reason = null)
    {
        if (_isForegroundVisible == visible
            && (_infoRoot == null || _infoRoot.activeSelf == visible)
            && (_buttonsRoot == null || _buttonsRoot.activeSelf == visible))
        {
            return;
        }

        _isForegroundVisible = visible;

        if (_infoRoot != null)
            _infoRoot.SetActive(visible);

        if (_buttonsRoot != null)
            _buttonsRoot.SetActive(visible);

        if (_presenter != null)
        {
            if (visible)
                _presenter.OnForegroundShown(reason);
            else
                _presenter.OnForegroundHidden(reason);
        }

        Debug.Log($"[LevelsScreen] Foreground -> {(visible ? "Visible" : "Hidden")} | Reason={reason ?? "Unspecified"} | Signals={GetForegroundSignalSummary()} | BlockingStack={UIPanel.GetBlockingPanelDebugSummary()} | ScreenVisible={_isVisible}");
    }

    private bool ShouldForegroundBeVisible()
    {
        return !_isWaitingForStartupSequence && !_isBlockedByForegroundSignal;
    }

    public bool IsForegroundVisible => _isForegroundVisible;

    private void ResolveDependencies()
    {
        if (_presenter == null)
            _presenter = GetComponent<LevelSelectionScreenController>() ?? GetComponentInChildren<LevelSelectionScreenController>(true);

        if (_buttonManager == null)
            _buttonManager = GetComponentInParent<ButtonManager>(true);

        if (_presenter == null)
            Debug.LogWarning("[LevelsScreen] LevelSelectionScreenController was not found.");

        if (_buttonManager == null)
            Debug.LogWarning("[LevelsScreen] ButtonManager was not found.");
    }

    private void SyncExternalForegroundSignals(string reason)
    {
        ApplyBlockingPanelSignal(IsBlockedByHigherPanel, $"{reason}/SyncBlockingPanel");
    }

    private void ApplyStartupSequenceSignal(bool active, string reason)
    {
        if (_isWaitingForStartupSequence == active)
        {
            Debug.Log($"[LevelsScreen] Signal -> StartupSequence unchanged | Active={active} | Reason={reason} | Signals={GetForegroundSignalSummary()}");
            RefreshForegroundVisibility($"{reason}/StartupSequenceUnchanged");
            return;
        }

        _isWaitingForStartupSequence = active;
        Debug.Log($"[LevelsScreen] Signal -> StartupSequence {(active ? "Requested" : "Released")} | Reason={reason} | Signals={GetForegroundSignalSummary()}");
        RefreshForegroundVisibility(reason);
    }

    private void ApplyBlockingPanelSignal(bool active, string reason)
    {
        if (_isBlockedByForegroundSignal == active)
        {
            Debug.Log($"[LevelsScreen] Signal -> BlockingPanel unchanged | Active={active} | Reason={reason} | Stack={UIPanel.GetBlockingPanelDebugSummary()} | Signals={GetForegroundSignalSummary()}");
            RefreshForegroundVisibility($"{reason}/BlockingPanelUnchanged");
            return;
        }

        _isBlockedByForegroundSignal = active;
        Debug.Log($"[LevelsScreen] Signal -> BlockingPanel {(active ? "Requested" : "Released")} | Reason={reason} | Stack={UIPanel.GetBlockingPanelDebugSummary()} | Signals={GetForegroundSignalSummary()}");
        RefreshForegroundVisibility(reason);
    }

    private string GetForegroundSignalSummary()
    {
        return $"StartupSequence={_isWaitingForStartupSequence}, BlockingPanel={_isBlockedByForegroundSignal}";
    }
}
