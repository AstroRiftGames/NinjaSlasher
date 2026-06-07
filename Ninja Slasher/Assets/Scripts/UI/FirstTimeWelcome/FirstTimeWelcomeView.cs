using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FirstTimeWelcomeView : UIOverlayBase
{
    [SerializeField] private Image _overlayImage;
    [SerializeField] private RectTransform _highlightRoot;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _skipButton;
    [SerializeField] private Vector2 _highlightPadding = new Vector2(18f, 18f);
    [SerializeField] private Animator _messagePanelAnimator;
    [SerializeField] private string _messageOpenStateName = "Open";
    [SerializeField] private string _messageCloseStateName = "Close";
    [SerializeField] private float _messageOpenFallbackDuration = 0.25f;
    [SerializeField] private float _messageCloseFallbackDuration = 0.2f;

    private readonly Vector3[] _targetCorners = new Vector3[4];
    private readonly Vector2[] _canvasPoints = new Vector2[4];
    private RectTransform _rootRect;
    private Canvas _canvas;
    private Camera _canvasCamera;
    private Coroutine _messageTransitionRoutine;
    private bool _isSubmitting;

    public event Action NextRequested;
    public event Action SkipRequested;

    protected override void Awake()
    {
        if (_backgroundImage == null)
            _backgroundImage = _overlayImage;

        base.Awake();
        ResolveMessagePanelAnimator();
        ConfigureInteractionLayers();
        ResolveCanvas();
        ValidateRequiredReferences();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ResolveMessagePanelAnimator();
        ConfigureInteractionLayers();
        RegisterButtonListeners();
    }

    protected override void OnDisable()
    {
        if (_messageTransitionRoutine != null)
        {
            StopCoroutine(_messageTransitionRoutine);
            _messageTransitionRoutine = null;
        }

        _isSubmitting = false;
        UnregisterButtonListeners();
        base.OnDisable();
    }

    public void ShowStep(string message, RectTransform target)
    {
        if (_messageTransitionRoutine != null)
        {
            StopCoroutine(_messageTransitionRoutine);
            _messageTransitionRoutine = null;
        }

        _isSubmitting = true;
        SetGuideButtonsInteractable(false);

        if (_messageText != null)
            _messageText.text = message ?? string.Empty;

        ConfigureInteractionLayers();
        UpdateHighlight(target);
        _messageTransitionRoutine = StartCoroutine(PlayMessageOpenAfterLayoutRoutine());
    }

    public override void Show()
    {
        ConfigureInteractionLayers();
        base.Show();
    }

    protected override void AnimateShow()
    {
        DOTween.Kill(_backgroundImage);
        DOTween.Kill(_canvasGroup);
        DOTween.Kill(_panelTransform);

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        if (_panelTransform != null)
            _panelTransform.localScale = Vector3.one;

        if (_backgroundImage == null)
        {
            OnShowAnimationCompleted();
            return;
        }

        RectTransform backgroundRect = _backgroundImage.rectTransform;
        if (backgroundRect != null)
        {
            backgroundRect.localScale = Vector3.one;
            backgroundRect.anchoredPosition = Vector2.zero;
        }

        float targetAlpha = _backgroundColor.a > 0.001f ? _backgroundColor.a : 0.7f;
        _backgroundImage.color = new Color(
            _backgroundColor.r,
            _backgroundColor.g,
            _backgroundColor.b,
            0f);

        if (_backgroundFadeDuration <= 0f)
        {
            _backgroundImage.color = new Color(
                _backgroundColor.r,
                _backgroundColor.g,
                _backgroundColor.b,
                targetAlpha);
            OnShowAnimationCompleted();
            return;
        }

        _backgroundImage
            .DOFade(targetAlpha, _backgroundFadeDuration)
            .SetEase(_fadeEase)
            .SetUpdate(true)
            .OnComplete(OnShowAnimationCompleted);
    }

    private void UpdateHighlight(RectTransform target)
    {
        if (_highlightRoot == null)
            return;

        if (target == null || _rootRect == null)
        {
            _highlightRoot.gameObject.SetActive(false);
            return;
        }

        _highlightRoot.gameObject.SetActive(true);
        target.GetWorldCorners(_targetCorners);

        for (int i = 0; i < _targetCorners.Length; i++)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootRect,
                RectTransformUtility.WorldToScreenPoint(_canvasCamera, _targetCorners[i]),
                _canvasCamera,
                out _canvasPoints[i]);
        }

        Vector2 min = _canvasPoints[0];
        Vector2 max = _canvasPoints[0];
        for (int i = 1; i < _canvasPoints.Length; i++)
        {
            Vector2 point = _canvasPoints[i];
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        Vector2 size = max - min + _highlightPadding;
        _highlightRoot.anchoredPosition = (min + max) * 0.5f;
        _highlightRoot.sizeDelta = size;
    }

    private void ResolveCanvas()
    {
        _canvas = GetComponentInParent<Canvas>();
        _rootRect = _canvas != null ? _canvas.transform as RectTransform : transform as RectTransform;
        _canvasCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _canvas.worldCamera
            : null;
    }

    private void ValidateRequiredReferences()
    {
        if (_canvasGroup == null || _panelTransform == null || (_backgroundImage == null && _overlayImage == null) ||
            _highlightRoot == null || _messageText == null || _nextButton == null || _skipButton == null)
        {
            Debug.LogWarning("[FirstTimeWelcomeView] Missing serialized UI references. Configure the view in the prefab.");
        }
    }

    private void ResolveMessagePanelAnimator()
    {
        if (_messagePanelAnimator != null)
            return;

        Transform controlsRoot = ResolveControlsRoot();
        if (controlsRoot != null)
            _messagePanelAnimator = controlsRoot.GetComponent<Animator>();

        if (_messagePanelAnimator == null && _messageText != null)
            _messagePanelAnimator = _messageText.GetComponentInParent<Animator>(true);
    }

    private void ConfigureInteractionLayers()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.ignoreParentGroups = true;
        }

        if (_overlayImage != null)
        {
            _overlayImage.raycastTarget = true;
            _overlayImage.transform.SetAsFirstSibling();
        }

        SetGraphicRaycastTarget(_highlightRoot, false);

        if (_messageText != null)
            _messageText.raycastTarget = false;

        Transform controlsRoot = ResolveControlsRoot();
        if (controlsRoot != null)
            controlsRoot.SetAsLastSibling();

        if (_nextButton != null)
        {
            _nextButton.interactable = !_isSubmitting;
            SetButtonGraphicRaycastTarget(_nextButton, true);
        }

        if (_skipButton != null)
        {
            _skipButton.interactable = !_isSubmitting;
            SetButtonGraphicRaycastTarget(_skipButton, true);
        }
    }

    private void SetGuideButtonsInteractable(bool interactable)
    {
        if (_nextButton != null)
            _nextButton.interactable = interactable;

        if (_skipButton != null)
            _skipButton.interactable = interactable;
    }

    private Transform ResolveControlsRoot()
    {
        if (_nextButton != null && _nextButton.transform.parent != null)
            return _nextButton.transform.parent;

        if (_skipButton != null && _skipButton.transform.parent != null)
            return _skipButton.transform.parent;

        if (_messageText != null && _messageText.transform.parent != null)
            return _messageText.transform.parent;

        return null;
    }

    private static void SetGraphicRaycastTarget(Component root, bool raycastTarget)
    {
        if (root == null)
            return;

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic != null)
                graphic.raycastTarget = raycastTarget;
        }
    }

    private static void SetButtonGraphicRaycastTarget(Button button, bool raycastTarget)
    {
        if (button == null)
            return;

        Graphic targetGraphic = button.targetGraphic;
        if (targetGraphic != null)
            targetGraphic.raycastTarget = raycastTarget;
    }

    private void RegisterButtonListeners()
    {
        _nextButton?.onClick.RemoveListener(OnNextClicked);
        _nextButton?.onClick.AddListener(OnNextClicked);

        _skipButton?.onClick.RemoveListener(OnSkipClicked);
        _skipButton?.onClick.AddListener(OnSkipClicked);
    }

    private void UnregisterButtonListeners()
    {
        _nextButton?.onClick.RemoveListener(OnNextClicked);
        _skipButton?.onClick.RemoveListener(OnSkipClicked);
    }

    private IEnumerator PlayMessageOpenAfterLayoutRoutine()
    {
        ResolveMessagePanelAnimator();
        ForceMessagePanelLayout();

        yield return null;

        if (_messagePanelAnimator == null || string.IsNullOrWhiteSpace(_messageOpenStateName))
        {
            _isSubmitting = false;
            SetGuideButtonsInteractable(true);
            _messageTransitionRoutine = null;
            yield break;
        }

        yield return PlayMessageStateRoutine(
            _messageOpenStateName,
            _messageOpenFallbackDuration,
            unlockButtonsOnComplete: true,
            onComplete: null);
    }

    private void OnNextClicked()
    {
        SubmitAfterClose(NextRequested);
    }

    private void OnSkipClicked()
    {
        SubmitAfterClose(SkipRequested);
    }

    private void SubmitAfterClose(Action callback)
    {
        if (_isSubmitting)
            return;

        _isSubmitting = true;
        SetGuideButtonsInteractable(false);

        if (_messageTransitionRoutine != null)
        {
            StopCoroutine(_messageTransitionRoutine);
            _messageTransitionRoutine = null;
        }

        ResolveMessagePanelAnimator();
        if (_messagePanelAnimator == null || string.IsNullOrWhiteSpace(_messageCloseStateName))
        {
            callback?.Invoke();
            return;
        }

        _messageTransitionRoutine = StartCoroutine(PlayMessageStateRoutine(
            _messageCloseStateName,
            _messageCloseFallbackDuration,
            unlockButtonsOnComplete: false,
            onComplete: callback));
    }

    private IEnumerator PlayMessageStateRoutine(string stateName, float fallbackDuration, bool unlockButtonsOnComplete, Action onComplete)
    {
        if (_messagePanelAnimator == null)
        {
            _messageTransitionRoutine = null;
            onComplete?.Invoke();
            yield break;
        }

        yield return PlayAnimatorTriggerAndWait(stateName, fallbackDuration);

        _messageTransitionRoutine = null;

        if (unlockButtonsOnComplete)
        {
            _isSubmitting = false;
            SetGuideButtonsInteractable(true);
        }

        onComplete?.Invoke();
    }

    private IEnumerator PlayAnimatorTriggerAndWait(string triggerName, float fallbackDuration)
    {
        const int layer = 0;

        _messagePanelAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        int initialStateHash = _messagePanelAnimator.GetCurrentAnimatorStateInfo(layer).fullPathHash;
        _messagePanelAnimator.ResetTrigger(_messageOpenStateName);
        _messagePanelAnimator.ResetTrigger(_messageCloseStateName);
        _messagePanelAnimator.SetTrigger(triggerName);

        float timeout = Mathf.Max(0.1f, fallbackDuration + 0.5f);
        float elapsed = 0f;
        bool observedPlayback = false;

        yield return null;

        while (elapsed < timeout)
        {
            if (_messagePanelAnimator == null || !_messagePanelAnimator.isActiveAndEnabled)
                yield break;

            AnimatorStateInfo state = _messagePanelAnimator.GetCurrentAnimatorStateInfo(layer);
            if (_messagePanelAnimator.IsInTransition(layer) || state.fullPathHash != initialStateHash)
            {
                observedPlayback = true;
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!observedPlayback)
        {
            yield return WaitForSecondsUnscaled(fallbackDuration);
            yield break;
        }

        elapsed = 0f;
        while (elapsed < timeout)
        {
            if (_messagePanelAnimator == null || !_messagePanelAnimator.isActiveAndEnabled)
                yield break;

            AnimatorStateInfo state = _messagePanelAnimator.GetCurrentAnimatorStateInfo(layer);
            if (!_messagePanelAnimator.IsInTransition(layer) && state.normalizedTime >= 1f)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void ForceMessagePanelLayout()
    {
        if (_messageText != null)
            _messageText.ForceMeshUpdate();

        Transform controlsRoot = ResolveControlsRoot();
        RectTransform controlsRect = controlsRoot as RectTransform;
        if (controlsRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(controlsRect);

        Canvas.ForceUpdateCanvases();
    }
}
