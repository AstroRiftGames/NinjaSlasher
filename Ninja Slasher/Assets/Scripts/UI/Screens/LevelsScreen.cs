using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LevelsScreen : UIScreenBase
{
    [SerializeField] private float _delayBeforeAnimation = 0.3f;
    [SerializeField] private float _postTransitionStarRevealDelay = 0.15f;
    [SerializeField] private GameObject _infoRoot;
    [SerializeField] private GameObject _buttonsRoot;
    [SerializeField] private AreaSectionController[] _areaSections;
    [SerializeField] private LevelSelectionScreenController _presenter;
    [SerializeField] private ButtonManager _buttonManager;
    [Header("Foreground Transition")]
    [SerializeField] private float _foregroundFadeDuration = 0.2f;
    [SerializeField] private Ease _foregroundShowEase = Ease.OutQuad;
    [SerializeField] private Ease _foregroundHideEase = Ease.InQuad;

    private bool _isWaitingForStartupSequence = false;
    private bool _isBlockedByForegroundSignal = false;
    private bool _isForegroundVisible = true;
    private Coroutine _pendingStarRevealRoutine;
    private CanvasGroup _infoRootCanvasGroup;
    private CanvasGroup _buttonsRootCanvasGroup;
    private Tween _infoRootTween;
    private Tween _buttonsRootTween;

    protected override void Awake()
    {
        base.Awake();
        ResolveDependencies();
        ResolveForegroundCanvasGroups();
        ApplyForegroundStateImmediate(_isForegroundVisible);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ResolveDependencies();
        ResolveForegroundCanvasGroups();
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
        CancelPendingStarReveal("OnDisable");
        _buttonManager?.StopStarRevealPresentation();
        KillForegroundTweens();

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LevelsScreen] Reveal -> AreaUnlocked | Buttons={buttons.Length}");
#endif
        _buttonManager.AnimateLevelButtonsReveal(buttons, "AreaUnlocked");
    }

    public override void Show()
    {
        bool wasNotVisible = !_isVisible;

        gameObject.SetActive(true);
        _isVisible = true;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        ResolveDependencies();
        ResolveForegroundCanvasGroups();
        SyncExternalForegroundSignals("Show");

        if (wasNotVisible)
        {
            if (ShouldPlayStartupReveal())
            {
                EnterStartupSuppressedState("Show/SessionBootstrapPending");
                HideLevelButtons();
            }
            else
            {
                ExitStartupSuppressedState("Show/SessionBootstrapCompleted");
                ShowLevelButtonsInstantly();
                SchedulePendingStarReveal("Show/SessionBootstrapCompleted");
            }
        }

        ApplyForegroundStateImmediate(ShouldForegroundBeVisible());
        NotifyPanelShown();

        if (wasNotVisible)
            OnShown();
    }

    protected override void OnShown()
    {
        MusicEvents.OnEnterLevelSelection?.Invoke();
    }

    public override void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;

        SetPanelInputEnabled(false);
        CancelPendingStarReveal("Hide");
        _buttonManager?.StopStarRevealPresentation();

        _buttonManager?.StopAllButtonAnimations();

        OnHidden();

        gameObject.SetActive(false);
    }

    private void OnStartupSequenceCompleted()
    {
        if (!_isWaitingForStartupSequence || !isActiveAndEnabled)
            return;

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LevelsScreen] Reveal -> IntroSequence | Buttons={visibleButtons.Count}");
#endif
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
        PlayForegroundTransition(visible);

        if (_presenter != null)
        {
            if (visible)
                _presenter.OnForegroundShown(reason);
            else
                _presenter.OnForegroundHidden(reason);
        }

        if (visible)
        {
            SchedulePendingStarReveal($"ForegroundVisible:{reason ?? "Unspecified"}");
        }
        else
        {
            CancelPendingStarReveal($"ForegroundHidden:{reason ?? "Unspecified"}");
            _buttonManager?.StopStarRevealPresentation();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LevelsScreen] Foreground -> {(visible ? "Visible" : "Hidden")} | Reason={reason ?? "Unspecified"} | Signals={GetForegroundSignalSummary()} | BlockingStack={UIPanel.GetBlockingPanelDebugSummary()} | ScreenVisible={_isVisible}");
#endif
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

    private void ResolveForegroundCanvasGroups()
    {
        _infoRootCanvasGroup = GetOrCreateCanvasGroup(_infoRoot, _infoRootCanvasGroup);
        _buttonsRootCanvasGroup = GetOrCreateCanvasGroup(_buttonsRoot, _buttonsRootCanvasGroup);
    }

    private static CanvasGroup GetOrCreateCanvasGroup(GameObject root, CanvasGroup current)
    {
        if (root == null)
            return null;

        if (current != null)
            return current;

        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        return canvasGroup != null ? canvasGroup : root.AddComponent<CanvasGroup>();
    }

    private void PlayForegroundTransition(bool visible)
    {
        ResolveForegroundCanvasGroups();
        KillForegroundTweens();

        PlayForegroundRootTransition(_infoRoot, _infoRootCanvasGroup, visible);
        PlayForegroundRootTransition(_buttonsRoot, _buttonsRootCanvasGroup, visible);
    }

    private void PlayForegroundRootTransition(GameObject root, CanvasGroup canvasGroup, bool visible)
    {
        if (root == null)
            return;

        if (canvasGroup == null)
        {
            root.SetActive(visible);
            return;
        }

        if (visible)
        {
            root.SetActive(true);
            canvasGroup.alpha = Mathf.Clamp01(canvasGroup.alpha);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Tween tween = canvasGroup.DOFade(1f, _foregroundFadeDuration)
                .SetEase(_foregroundShowEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                });

            AssignForegroundTween(root, tween);
            return;
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Tween hideTween = canvasGroup.DOFade(0f, _foregroundFadeDuration)
            .SetEase(_foregroundHideEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                canvasGroup.alpha = 0f;
                root.SetActive(false);
            });

        AssignForegroundTween(root, hideTween);
    }

    private void AssignForegroundTween(GameObject root, Tween tween)
    {
        if (root == _infoRoot)
            _infoRootTween = tween;
        else if (root == _buttonsRoot)
            _buttonsRootTween = tween;
    }

    private void KillForegroundTweens()
    {
        if (_infoRootCanvasGroup != null)
            _infoRootCanvasGroup.DOKill();

        if (_buttonsRootCanvasGroup != null)
            _buttonsRootCanvasGroup.DOKill();

        if (_infoRootTween != null)
        {
            _infoRootTween.Kill();
            _infoRootTween = null;
        }

        if (_buttonsRootTween != null)
        {
            _buttonsRootTween.Kill();
            _buttonsRootTween = null;
        }
    }

    private void ApplyForegroundStateImmediate(bool visible)
    {
        ResolveForegroundCanvasGroups();
        KillForegroundTweens();
        _isForegroundVisible = visible;
        ApplyForegroundRootStateImmediate(_infoRoot, _infoRootCanvasGroup, visible);
        ApplyForegroundRootStateImmediate(_buttonsRoot, _buttonsRootCanvasGroup, visible);
    }

    private static void ApplyForegroundRootStateImmediate(GameObject root, CanvasGroup canvasGroup, bool visible)
    {
        if (root == null)
            return;

        root.SetActive(visible);

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private bool ShouldPlayStartupReveal()
    {
        UIManager uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            Debug.LogWarning("[LevelsScreen] UIManager was not found. Startup reveal will remain enabled by default.");
            return true;
        }

        return uiManager.ShouldRunLevelSelectionStartupFlowOnNextEntry();
    }

    private void SchedulePendingStarReveal(string reason)
    {
        if (_buttonManager == null || !isActiveAndEnabled || !_isVisible || !_isForegroundVisible)
            return;

        CancelPendingStarReveal($"{reason}/Reschedule");
        _pendingStarRevealRoutine = StartCoroutine(PlayPendingStarRevealAfterDelay(reason));
    }

    private void CancelPendingStarReveal(string reason)
    {
        if (_pendingStarRevealRoutine == null)
            return;

        StopCoroutine(_pendingStarRevealRoutine);
        _pendingStarRevealRoutine = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LevelsScreen] StarReveal -> Cancelled | Reason={reason}");
#endif
    }

    private IEnumerator PlayPendingStarRevealAfterDelay(string reason)
    {
        if (_postTransitionStarRevealDelay > 0f)
            yield return WaitForSecondsUnscaled(_postTransitionStarRevealDelay);

        _pendingStarRevealRoutine = null;

        if (!isActiveAndEnabled || !_isVisible || !_isForegroundVisible)
            yield break;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LevelsScreen] StarReveal -> Triggered | Reason={reason}");
#endif
        float completionFeedbackDuration = 0f;
        if (_buttonManager != null)
            _buttonManager.TryPlayPendingCompletionAnimations(out completionFeedbackDuration);

        if (completionFeedbackDuration > 0f)
            yield return WaitForSecondsUnscaled(completionFeedbackDuration);

        if (!isActiveAndEnabled || !_isVisible || !_isForegroundVisible)
            yield break;

        _presenter?.TryPlayPendingAreaUnlockFeedback();
    }

    private void SyncExternalForegroundSignals(string reason)
    {
        ApplyBlockingPanelSignal(IsBlockedByHigherPanel, $"{reason}/SyncBlockingPanel");
    }

    private void ApplyStartupSequenceSignal(bool active, string reason)
    {
        if (_isWaitingForStartupSequence == active)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[LevelsScreen] Signal -> StartupSequence unchanged | Active={active} | Reason={reason} | Signals={GetForegroundSignalSummary()}");
#endif
            RefreshForegroundVisibility($"{reason}/StartupSequenceUnchanged");
            return;
        }

        _isWaitingForStartupSequence = active;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LevelsScreen] Signal -> StartupSequence {(active ? "Requested" : "Released")} | Reason={reason} | Signals={GetForegroundSignalSummary()}");
#endif
        RefreshForegroundVisibility(reason);
    }

    private void ApplyBlockingPanelSignal(bool active, string reason)
    {
        if (_isBlockedByForegroundSignal == active)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[LevelsScreen] Signal -> BlockingPanel unchanged | Active={active} | Reason={reason} | Stack={UIPanel.GetBlockingPanelDebugSummary()} | Signals={GetForegroundSignalSummary()}");
#endif
            RefreshForegroundVisibility($"{reason}/BlockingPanelUnchanged");
            return;
        }

        _isBlockedByForegroundSignal = active;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LevelsScreen] Signal -> BlockingPanel {(active ? "Requested" : "Released")} | Reason={reason} | Stack={UIPanel.GetBlockingPanelDebugSummary()} | Signals={GetForegroundSignalSummary()}");
#endif
        RefreshForegroundVisibility(reason);
    }

    private string GetForegroundSignalSummary()
    {
        return $"StartupSequence={_isWaitingForStartupSequence}, BlockingPanel={_isBlockedByForegroundSignal}";
    }
}
