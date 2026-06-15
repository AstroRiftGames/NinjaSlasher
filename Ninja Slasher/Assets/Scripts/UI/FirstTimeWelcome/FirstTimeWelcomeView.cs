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
    [SerializeField] private TextMeshProUGUI _nextButtonLabel;
    [SerializeField] private TextMeshProUGUI _skipButtonLabel;
    [SerializeField] private Button _welcomeNextButton;
    [SerializeField] private Button _welcomeSkipButton;
    [SerializeField] private TextMeshProUGUI _welcomeNextButtonLabel;
    [SerializeField] private TextMeshProUGUI _welcomeSkipButtonLabel;
    [SerializeField] private Vector2 _highlightPadding = new Vector2(18f, 18f);

    [Header("Message Card")]
    [SerializeField] private RectTransform _messageCardTransform;
    [SerializeField] private CanvasGroup _messageCardCanvasGroup;
    [SerializeField] private RectTransform _welcomeMessageCardTransform;
    [SerializeField] private RectTransform _canvasRect;
    [SerializeField] private Vector2 _defaultMessagePosition = new Vector2(0f, -450f);
    [SerializeField] private Vector2 _targetOffset = new Vector2(40f, 40f);
    [SerializeField] private Vector2 _screenPadding = new Vector2(40f, 40f);
    [SerializeField] private float _openDuration = 0.25f;
    [SerializeField] private float _closeDuration = 0.2f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InQuad;
    [SerializeField] private TextMeshProUGUI _welcomeMessageCardText;

    [SerializeField] private Animator _welcomeMessageCardAnimator; 
    [SerializeField] private string _welcomeOpenStateName = "Open-Welcome";
    [SerializeField] private string _welcomeCloseStateName = "Close-Welcome";

    private Coroutine _welcomeAnimationRoutine;

    private static readonly int WelcomeOpenTrigger = Animator.StringToHash("Open");
    private static readonly int WelcomeCloseTrigger = Animator.StringToHash("Close");

    private readonly Vector3[] _targetCorners = new Vector3[4];
    private readonly Vector2[] _canvasPoints = new Vector2[4];
    private RectTransform _rootRect;
    private Canvas _canvas;
    private Camera _canvasCamera;
    private Sequence _messageCardSequence;
    private bool _isSubmitting;
    private bool _isShowing;

    public bool IsShowing
    {
        get
        {
            bool hasSeen = false;
            if (SaveManager.Instance != null && SaveManager.Instance.IsDataLoaded)
            {
                var data = SaveManager.Instance.GetGameData();
                if (data != null)
                {
                    hasSeen = data.hasSeenFirstTimeWelcome;
                }
            }
            if (hasSeen)
            {
                _isShowing = false;
                return false;
            }
            return _isShowing && gameObject.activeInHierarchy;
        }
    }

    public event Action NextRequested;
    public event Action SkipRequested;
    public event Action WelcomeNextRequested;
    public event Action WelcomeSkipRequested;

    protected override void Awake()
    {
        if (_backgroundImage == null)
            _backgroundImage = _overlayImage;

        base.Awake();
        ConfigureInteractionLayers();
        ResolveCanvas();
        ResolveMessageCardReferences();
        ResolveButtonLabels();
        SetMessageCardHidden(_defaultMessagePosition);
        SetWelcomeMessageCardHidden(Vector2.zero);
        ValidateRequiredReferences();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ConfigureInteractionLayers();
        RegisterButtonListeners();
    }

    protected override void OnDisable()
    {
        StopWelcomeAnimationRoutine();
        KillMessageCardTweens();
        SetMessageCardHidden(_defaultMessagePosition);
        SetWelcomeMessageCardHidden(Vector2.zero);
        _isSubmitting = false;
        UnregisterButtonListeners();
        _isShowing = false;
        base.OnDisable();
    }

    public void Warmup()
    {
        ResolveCanvas();
        ResolveMessageCardReferences();
        KillMessageCardTweens();
        SetMessageCardHidden(_defaultMessagePosition);
        SetWelcomeMessageCardHidden(Vector2.zero);

        if (_messageText != null)
            _messageText.ForceMeshUpdate();

        if (_welcomeMessageCardText != null)
            _welcomeMessageCardText.ForceMeshUpdate();
    }

    public void ShowStep(string message, RectTransform target, bool useWelcomePanel, bool showSkipButton, string nextButtonLabel)
    {
        KillMessageCardTweens();
        _isSubmitting = true;
        SetGuideButtonsInteractable(false);
        ConfigureStepButtons(useWelcomePanel, showSkipButton, nextButtonLabel);

        SetActiveMessagePanel(useWelcomePanel);

        if (useWelcomePanel)
        {
            if (_welcomeMessageCardText != null)
                _welcomeMessageCardText.text = message ?? string.Empty;
        }
        else
        {
            if (_messageText != null)
                _messageText.text = message ?? string.Empty;
        }

        UpdateHighlight(useWelcomePanel ? null : target);
        PositionMessageCard(target, useWelcomePanel);
        PlayMessageOpen();
    }

    public override void Show()
    {
        bool hasSeen = false;
        if (SaveManager.Instance != null && SaveManager.Instance.IsDataLoaded)
        {
            var data = SaveManager.Instance.GetGameData();
            if (data != null)
            {
                hasSeen = data.hasSeenFirstTimeWelcome;
            }
        }
        if (hasSeen)
        {
            _isShowing = false;
            gameObject.SetActive(false);
            return;
        }

        _isShowing = true;
        ConfigureInteractionLayers();
        base.Show();
    }

    protected override void OnHideAnimationCompleted()
    {
        base.OnHideAnimationCompleted();
        _isShowing = false;
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

    protected override void AnimateHide()
    {
        KillMessageCardTweens();
        SetGuideButtonsInteractable(false);
        SetMessageCardHidden(_messageCardTransform != null
            ? _messageCardTransform.anchoredPosition
            : _defaultMessagePosition);
        SetWelcomeMessageCardHidden(_welcomeMessageCardTransform != null
            ? _welcomeMessageCardTransform.anchoredPosition
            : Vector2.zero);
        base.AnimateHide();
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
        if (_canvasRect == null)
            _canvasRect = _rootRect;

        _canvasCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _canvas.worldCamera
            : null;
    }

    private void ValidateRequiredReferences()
    {
        if (_canvasGroup == null || _panelTransform == null || (_backgroundImage == null && _overlayImage == null) ||
            _highlightRoot == null || _messageText == null || _nextButton == null || _skipButton == null ||
            _welcomeNextButton == null || _welcomeSkipButton == null ||
            _messageCardTransform == null || _messageCardCanvasGroup == null ||
            _welcomeMessageCardTransform == null || _welcomeMessageCardAnimator == null || _nextButtonLabel == null || _skipButtonLabel == null ||
            _welcomeNextButtonLabel == null || _welcomeSkipButtonLabel == null ||
            _welcomeMessageCardText == null || _canvasRect == null)
        {
            Debug.LogWarning("[FirstTimeWelcomeView] Missing serialized UI references. Configure the view in the prefab.");
        }
    }

    private void ResolveMessageCardReferences()
    {
        if (_messageCardTransform == null && _messageText != null)
        {
            Transform current = _messageText.transform;
            while (current != null && current != transform)
            {
                if (current.name == "MessageCard")
                {
                    _messageCardTransform = current as RectTransform;
                    break;
                }

                current = current.parent;
            }
        }

        if (_messageCardCanvasGroup == null && _messageCardTransform != null)
            _messageCardCanvasGroup = _messageCardTransform.GetComponent<CanvasGroup>();

        if (_welcomeMessageCardTransform == null)
            _welcomeMessageCardTransform = FindChildRectTransformByName("WelcomeMessageCard");

        if (_welcomeMessageCardText == null && _welcomeMessageCardTransform != null)
            _welcomeMessageCardText = _welcomeMessageCardTransform.GetComponentInChildren<TextMeshProUGUI>(true);

        if (_welcomeMessageCardAnimator == null && _welcomeMessageCardTransform != null)
            _welcomeMessageCardAnimator = _welcomeMessageCardTransform.GetComponent<Animator>();
    }

    private RectTransform FindChildRectTransformByName(string childName)
    {
        if (string.IsNullOrWhiteSpace(childName))
            return null;

        RectTransform[] rectTransforms = GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rectTransforms.Length; i++)
        {
            RectTransform rectTransform = rectTransforms[i];
            if (rectTransform != null && rectTransform != transform && string.Equals(rectTransform.name, childName, StringComparison.Ordinal))
                return rectTransform;
        }

        return null;
    }

    private void ResolveButtonLabels()
    {
        if (_nextButtonLabel == null && _nextButton != null)
            _nextButtonLabel = _nextButton.GetComponentInChildren<TextMeshProUGUI>(true);

        if (_skipButtonLabel == null && _skipButton != null)
            _skipButtonLabel = _skipButton.GetComponentInChildren<TextMeshProUGUI>(true);

        if (_welcomeNextButtonLabel == null && _welcomeNextButton != null)
            _welcomeNextButtonLabel = _welcomeNextButton.GetComponentInChildren<TextMeshProUGUI>(true);

        if (_welcomeSkipButtonLabel == null && _welcomeSkipButton != null)
            _welcomeSkipButtonLabel = _welcomeSkipButton.GetComponentInChildren<TextMeshProUGUI>(true);
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

        if (_welcomeMessageCardText != null)
            _welcomeMessageCardText.raycastTarget = false;

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

        if (_welcomeNextButton != null)
        {
            _welcomeNextButton.interactable = !_isSubmitting;
            SetButtonGraphicRaycastTarget(_welcomeNextButton, true);
        }

        if (_welcomeSkipButton != null)
        {
            _welcomeSkipButton.interactable = !_isSubmitting;
            SetButtonGraphicRaycastTarget(_welcomeSkipButton, true);
        }
    }

    private void SetGuideButtonsInteractable(bool interactable)
    {
        if (_nextButton != null)
            _nextButton.interactable = interactable;

        if (_skipButton != null)
            _skipButton.interactable = interactable;

        if (_welcomeNextButton != null)
            _welcomeNextButton.interactable = interactable;

        if (_welcomeSkipButton != null)
            _welcomeSkipButton.interactable = interactable;
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

        _welcomeNextButton?.onClick.RemoveListener(OnWelcomeNextClicked);
        _welcomeNextButton?.onClick.AddListener(OnWelcomeNextClicked);

        _welcomeSkipButton?.onClick.RemoveListener(OnWelcomeSkipClicked);
        _welcomeSkipButton?.onClick.AddListener(OnWelcomeSkipClicked);
    }

    private void UnregisterButtonListeners()
    {
        _nextButton?.onClick.RemoveListener(OnNextClicked);
        _skipButton?.onClick.RemoveListener(OnSkipClicked);

        _welcomeNextButton?.onClick.RemoveListener(OnWelcomeNextClicked);
        _welcomeSkipButton?.onClick.RemoveListener(OnWelcomeSkipClicked);
    }

    private void SetActiveMessagePanel(bool useWelcomePanel)
    {
        if (_messageCardTransform != null)
            _messageCardTransform.gameObject.SetActive(!useWelcomePanel);

        if (_welcomeMessageCardTransform != null)
            _welcomeMessageCardTransform.gameObject.SetActive(useWelcomePanel);
    }

    private RectTransform GetActiveMessageCardTransform()
    {
        return _welcomeMessageCardTransform != null && _welcomeMessageCardTransform.gameObject.activeSelf
            ? _welcomeMessageCardTransform
            : _messageCardTransform;
    }

    private CanvasGroup GetContextualMessageCardCanvasGroup()
    {
        return _messageCardCanvasGroup;
    }

    private void ConfigureStepButtons(bool showSkipButton, string nextButtonLabel)
    {
        if (_nextButtonLabel != null)
            _nextButtonLabel.text = string.IsNullOrWhiteSpace(nextButtonLabel) ? "Siguiente" : nextButtonLabel;

        if (_skipButton != null)
            _skipButton.gameObject.SetActive(showSkipButton);
    }

    private void ConfigureStepButtons(bool useWelcomePanel, bool showSkipButton, string nextButtonLabel)
    {
        if (useWelcomePanel)
        {
            if (_welcomeNextButtonLabel != null)
                _welcomeNextButtonLabel.text = string.IsNullOrWhiteSpace(nextButtonLabel) ? "Siguiente" : nextButtonLabel;

            if (_welcomeSkipButton != null)
                _welcomeSkipButton.gameObject.SetActive(showSkipButton);
        }
        else
        {
            ConfigureStepButtons(showSkipButton, nextButtonLabel);
        }
    }

    private void PlayMessageOpen()
    {
        if (IsWelcomeMessagePanelActive())
        {
            PlayWelcomeMessageOpen();
            return;
        }

        RectTransform activeCard = GetActiveMessageCardTransform();
        CanvasGroup activeCanvasGroup = GetContextualMessageCardCanvasGroup();
        if (activeCard == null || activeCanvasGroup == null)
        {
            _isSubmitting = false;
            SetGuideButtonsInteractable(true);
            return;
        }

        activeCanvasGroup.alpha = 0f;
        activeCard.localScale = Vector3.one * 0.92f;

        _messageCardSequence = DOTween.Sequence()
            .SetTarget(activeCard)
            .SetUpdate(true);

        _messageCardSequence.Join(
            activeCanvasGroup
                .DOFade(1f, Mathf.Max(0f, _openDuration))
                .SetEase(Ease.OutQuad)
                .SetUpdate(true));

        _messageCardSequence.Join(
            activeCard
                .DOScale(1f, Mathf.Max(0f, _openDuration))
                .SetEase(_openEase)
                .SetUpdate(true));

        _messageCardSequence.OnComplete(() =>
        {
            _messageCardSequence = null;
            _isSubmitting = false;
            SetGuideButtonsInteractable(true);
        });
    }

    private void PlayWelcomeMessageOpen()
    {
        StopWelcomeAnimationRoutine();
        _welcomeAnimationRoutine = StartCoroutine(PlayWelcomeMessageOpenRoutine());
    }

    private IEnumerator PlayWelcomeMessageOpenRoutine()
    {
        RectTransform activeCard = GetActiveMessageCardTransform();

        if (_welcomeMessageCardAnimator == null || activeCard == null)
        {
            _isSubmitting = false;
            SetGuideButtonsInteractable(true);
            yield break;
        }

        _welcomeMessageCardAnimator.ResetTrigger(WelcomeCloseTrigger);
        _welcomeMessageCardAnimator.SetTrigger(WelcomeOpenTrigger);

        yield return WaitForAnimatorStateToComplete(_welcomeMessageCardAnimator, _welcomeOpenStateName);

        _welcomeAnimationRoutine = null;
        _isSubmitting = false;
        SetGuideButtonsInteractable(true);
    }

    private void OnNextClicked()
    {
        SubmitAfterClose(NextRequested);
    }

    private void OnWelcomeNextClicked()
    {
        SubmitAfterClose(WelcomeNextRequested);
    }

    private void OnSkipClicked()
    {
        SubmitAfterClose(SkipRequested);
    }

    private void OnWelcomeSkipClicked()
    {
        SubmitAfterClose(WelcomeSkipRequested);
    }

    private void SubmitAfterClose(Action callback)
    {
        if (_isSubmitting)
            return;

        _isSubmitting = true;
        SetGuideButtonsInteractable(false);
        KillMessageCardTweens();

        if (IsWelcomeMessagePanelActive())
        {
            SubmitWelcomeAfterClose(callback);
            return;
        }

        RectTransform activeCard = GetActiveMessageCardTransform();
        CanvasGroup activeCanvasGroup = GetContextualMessageCardCanvasGroup();
        if (activeCard == null || activeCanvasGroup == null)
        {
            callback?.Invoke();
            return;
        }

        _messageCardSequence = DOTween.Sequence()
            .SetTarget(activeCard)
            .SetUpdate(true);

        _messageCardSequence.Join(
            activeCanvasGroup
                .DOFade(0f, Mathf.Max(0f, _closeDuration))
                .SetEase(Ease.InQuad)
                .SetUpdate(true));

        _messageCardSequence.Join(
            activeCard
                .DOScale(0.92f, Mathf.Max(0f, _closeDuration))
                .SetEase(_closeEase)
                .SetUpdate(true));

        _messageCardSequence.OnComplete(() =>
        {
            _messageCardSequence = null;
            callback?.Invoke();
        });
    }

    private void SubmitWelcomeAfterClose(Action callback)
    {
        StopWelcomeAnimationRoutine();
        _welcomeAnimationRoutine = StartCoroutine(SubmitWelcomeAfterCloseRoutine(callback));
    }

    private IEnumerator SubmitWelcomeAfterCloseRoutine(Action callback)
    {
        RectTransform activeCard = GetActiveMessageCardTransform();

        if (_welcomeMessageCardAnimator == null || activeCard == null)
        {
            callback?.Invoke();
            yield break;
        }

        _welcomeMessageCardAnimator.ResetTrigger(WelcomeOpenTrigger);
        _welcomeMessageCardAnimator.SetTrigger(WelcomeCloseTrigger);

        yield return WaitForAnimatorStateToComplete(_welcomeMessageCardAnimator, _welcomeCloseStateName);

        _welcomeAnimationRoutine = null;
        callback?.Invoke();
    }

    private void PositionMessageCard(RectTransform target, bool useWelcomePanel)
    {
        RectTransform activeCard = GetActiveMessageCardTransform();
        if (activeCard == null || _canvasRect == null)
            return;

        Vector2 position;
        if (useWelcomePanel)
        {
            position = Vector2.zero;
        }
        else if (target != null)
        {
            position = CalculateContextualMessagePosition(target, activeCard);
        }
        else
        {
            position = ClampMessagePosition(_defaultMessagePosition, activeCard);
        }

        activeCard.anchoredPosition = position;
    }

    private Vector2 CalculateContextualMessagePosition(RectTransform target, RectTransform cardTransform)
    {
        Bounds2D targetBounds = GetTargetBounds(target);
        return CalculateContextualMessagePosition(targetBounds.min, targetBounds.max, cardTransform);
    }

    private Vector2 CalculateContextualMessagePosition(Vector2 targetMin, Vector2 targetMax, RectTransform cardTransform)
    {
        Vector2 targetCenter = (targetMin + targetMax) * 0.5f;
        Rect canvasBounds = _canvasRect.rect;
        Vector2 canvasCenter = canvasBounds.center;
        float normalizedX = (targetCenter.x - canvasCenter.x) / Mathf.Max(1f, canvasBounds.width);
        float normalizedY = (targetCenter.y - canvasCenter.y) / Mathf.Max(1f, canvasBounds.height);

        MessageSide primarySide;
        MessageSide secondarySide;
        if (Mathf.Abs(normalizedY) >= Mathf.Abs(normalizedX))
        {
            primarySide = normalizedY >= 0f ? MessageSide.Below : MessageSide.Above;
            secondarySide = normalizedX >= 0f ? MessageSide.Left : MessageSide.Right;
        }
        else
        {
            primarySide = normalizedX >= 0f ? MessageSide.Left : MessageSide.Right;
            secondarySide = normalizedY >= 0f ? MessageSide.Below : MessageSide.Above;
        }

        Vector2 primaryPosition = ClampMessagePosition(
            CalculatePositionForSide(primarySide, targetMin, targetMax, cardTransform), cardTransform);
        if (!DoesMessageOverlapTarget(primaryPosition, targetMin, targetMax))
            return primaryPosition;

        Vector2 secondaryPosition = ClampMessagePosition(
            CalculatePositionForSide(secondarySide, targetMin, targetMax, cardTransform), cardTransform);
        if (!DoesMessageOverlapTarget(secondaryPosition, targetMin, targetMax))
            return secondaryPosition;

        return primaryPosition;
    }

    private Bounds2D GetTargetBounds(RectTransform target)
    {
        target.GetWorldCorners(_targetCorners);
        for (int i = 0; i < _targetCorners.Length; i++)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                RectTransformUtility.WorldToScreenPoint(_canvasCamera, _targetCorners[i]),
                _canvasCamera,
                out _canvasPoints[i]);
        }

        Vector2 targetMin = _canvasPoints[0];
        Vector2 targetMax = _canvasPoints[0];
        for (int i = 1; i < _canvasPoints.Length; i++)
        {
            targetMin = Vector2.Min(targetMin, _canvasPoints[i]);
            targetMax = Vector2.Max(targetMax, _canvasPoints[i]);
        }

        return new Bounds2D(targetMin, targetMax);
    }

    private Vector2 CalculatePositionForSide(MessageSide side, Vector2 targetMin, Vector2 targetMax, RectTransform cardTransform)
    {
        Rect cardRect = cardTransform.rect;
        Vector2 pivot = cardTransform.pivot;
        Vector2 targetCenter = (targetMin + targetMax) * 0.5f;
        float horizontalOffset = Mathf.Abs(_targetOffset.x);
        float verticalOffset = Mathf.Abs(_targetOffset.y);

        switch (side)
        {
            case MessageSide.Above:
                return new Vector2(
                    targetCenter.x,
                    targetMax.y + verticalOffset + pivot.y * cardRect.height);
            case MessageSide.Below:
                return new Vector2(
                    targetCenter.x,
                    targetMin.y - verticalOffset - (1f - pivot.y) * cardRect.height);
            case MessageSide.Left:
                return new Vector2(
                    targetMin.x - horizontalOffset - (1f - pivot.x) * cardRect.width,
                    targetCenter.y);
            default:
                return new Vector2(
                    targetMax.x + horizontalOffset + pivot.x * cardRect.width,
                    targetCenter.y);
        }
    }

    private Vector2 ClampMessagePosition(Vector2 position, RectTransform cardTransform)
    {
        Rect canvasBounds = _canvasRect.rect;
        Rect cardRect = cardTransform.rect;
        Vector2 pivot = cardTransform.pivot;

        float minX = canvasBounds.xMin + Mathf.Abs(_screenPadding.x) + pivot.x * cardRect.width;
        float maxX = canvasBounds.xMax - Mathf.Abs(_screenPadding.x) - (1f - pivot.x) * cardRect.width;
        float minY = canvasBounds.yMin + Mathf.Abs(_screenPadding.y) + pivot.y * cardRect.height;
        float maxY = canvasBounds.yMax - Mathf.Abs(_screenPadding.y) - (1f - pivot.y) * cardRect.height;

        position.x = minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : canvasBounds.center.x;
        position.y = minY <= maxY ? Mathf.Clamp(position.y, minY, maxY) : canvasBounds.center.y;
        return position;
    }

    private bool DoesMessageOverlapTarget(Vector2 position, Vector2 targetMin, Vector2 targetMax)
    {
        RectTransform activeCard = GetActiveMessageCardTransform();
        if (activeCard == null)
            return false;

        Rect cardRect = activeCard.rect;
        Vector2 pivot = activeCard.pivot;
        float minX = position.x - pivot.x * cardRect.width;
        float maxX = position.x + (1f - pivot.x) * cardRect.width;
        float minY = position.y - pivot.y * cardRect.height;
        float maxY = position.y + (1f - pivot.y) * cardRect.height;

        return minX < targetMax.x && maxX > targetMin.x &&
               minY < targetMax.y && maxY > targetMin.y;
    }

    private void KillMessageCardTweens()
    {
        if (_messageCardTransform != null)
            DOTween.Kill(_messageCardTransform);

        if (_messageCardCanvasGroup != null)
            DOTween.Kill(_messageCardCanvasGroup);

        if (_messageCardSequence != null)
        {
            _messageCardSequence.Kill();
            _messageCardSequence = null;
        }
    }

    private void SetMessageCardHidden(Vector2 position)
    {
        if (_messageCardTransform != null)
        {
            _messageCardTransform.anchoredPosition = ClampMessagePosition(position, _messageCardTransform);
            _messageCardTransform.localScale = Vector3.one * 0.92f;
        }

        if (_messageCardCanvasGroup != null)
            _messageCardCanvasGroup.alpha = 0f;
    }

    private void SetWelcomeMessageCardHidden(Vector2 position)
    {
        if (_welcomeMessageCardTransform == null)
            return;

        _welcomeMessageCardTransform.anchoredPosition = ClampMessagePosition(position, _welcomeMessageCardTransform);
    }

    private enum MessageSide
    {
        Above,
        Below,
        Left,
        Right
    }

    private readonly struct Bounds2D
    {
        public readonly Vector2 min;
        public readonly Vector2 max;

        public Bounds2D(Vector2 min, Vector2 max)
        {
            this.min = min;
            this.max = max;
        }
    }

    private bool IsWelcomeMessagePanelActive()
    {
        return _welcomeMessageCardTransform != null &&
               _welcomeMessageCardTransform.gameObject.activeSelf;
    }

    private void StopWelcomeAnimationRoutine()
    {
        if (_welcomeAnimationRoutine == null)
            return;

        StopCoroutine(_welcomeAnimationRoutine);
        _welcomeAnimationRoutine = null;
    }

    private IEnumerator WaitForAnimatorStateToComplete(Animator animator, string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            yield break;

        yield return null;

        int safetyFrames = 0;
        const int maxSafetyFrames = 300;

        while (animator.isActiveAndEnabled && !animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
        {
            safetyFrames++;

            if (safetyFrames >= maxSafetyFrames)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[FirstTimeWelcomeView] Animator state '{stateName}' was not reached.");
#endif
                yield break;
            }

            yield return null;
        }

        while (animator.isActiveAndEnabled)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (!stateInfo.IsName(stateName))
                yield break;

            if (stateInfo.normalizedTime >= 1f && !animator.IsInTransition(0))
                yield break;

            yield return null;
        }
    }
}
