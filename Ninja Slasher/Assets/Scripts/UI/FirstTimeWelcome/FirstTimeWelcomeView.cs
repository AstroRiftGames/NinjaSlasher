using System;
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

    [Header("Message Card")]
    [SerializeField] private RectTransform _messageCardTransform;
    [SerializeField] private CanvasGroup _messageCardCanvasGroup;
    [SerializeField] private RectTransform _canvasRect;
    [SerializeField] private Vector2 _defaultMessagePosition = new Vector2(0f, -450f);
    [SerializeField] private Vector2 _targetOffset = new Vector2(40f, 40f);
    [SerializeField] private Vector2 _screenPadding = new Vector2(40f, 40f);
    [SerializeField] private float _openDuration = 0.25f;
    [SerializeField] private float _closeDuration = 0.2f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InQuad;
    [SerializeField] private TextMeshProUGUI _nextButtonLabel;
    [SerializeField] private TextMeshProUGUI _skipButtonLabel;

    private readonly Vector3[] _targetCorners = new Vector3[4];
    private readonly Vector2[] _canvasPoints = new Vector2[4];
    private RectTransform _rootRect;
    private Canvas _canvas;
    private Camera _canvasCamera;
    private Sequence _messageCardSequence;
    private bool _isSubmitting;

    public event Action NextRequested;
    public event Action SkipRequested;

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
        KillMessageCardTweens();
        SetMessageCardHidden(_defaultMessagePosition);
        _isSubmitting = false;
        UnregisterButtonListeners();
        base.OnDisable();
    }

    public void Warmup()
    {
        ResolveCanvas();
        ResolveMessageCardReferences();
        KillMessageCardTweens();
        SetMessageCardHidden(_defaultMessagePosition);

        if (_messageText != null)
            _messageText.ForceMeshUpdate();
    }

    public void ShowStep(string message, RectTransform target, bool forceCenteredPosition, bool showSkipButton, string nextButtonLabel)
    {
        KillMessageCardTweens();
        _isSubmitting = true;
        SetGuideButtonsInteractable(false);
        ConfigureStepButtons(showSkipButton, nextButtonLabel);

        if (_messageText != null)
            _messageText.text = message ?? string.Empty;

        UpdateHighlight(target);
        PositionMessageCard(target, forceCenteredPosition);
        PlayMessageOpen();
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

    protected override void AnimateHide()
    {
        KillMessageCardTweens();
        SetGuideButtonsInteractable(false);
        SetMessageCardHidden(_messageCardTransform != null
            ? _messageCardTransform.anchoredPosition
            : _defaultMessagePosition);
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
            _messageCardTransform == null || _messageCardCanvasGroup == null || _canvasRect == null)
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
    }

    private void ResolveButtonLabels()
    {
        if (_nextButtonLabel == null && _nextButton != null)
            _nextButtonLabel = _nextButton.GetComponentInChildren<TextMeshProUGUI>(true);

        if (_skipButtonLabel == null && _skipButton != null)
            _skipButtonLabel = _skipButton.GetComponentInChildren<TextMeshProUGUI>(true);
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

    private void ConfigureStepButtons(bool showSkipButton, string nextButtonLabel)
    {
        if (_nextButtonLabel != null)
            _nextButtonLabel.text = string.IsNullOrWhiteSpace(nextButtonLabel) ? "Siguiente" : nextButtonLabel;

        if (_skipButton != null)
            _skipButton.gameObject.SetActive(showSkipButton);
    }

    private void PlayMessageOpen()
    {
        if (_messageCardTransform == null || _messageCardCanvasGroup == null)
        {
            _isSubmitting = false;
            SetGuideButtonsInteractable(true);
            return;
        }

        _messageCardCanvasGroup.alpha = 0f;
        _messageCardTransform.localScale = Vector3.one * 0.92f;

        _messageCardSequence = DOTween.Sequence()
            .SetTarget(_messageCardTransform)
            .SetUpdate(true);
        _messageCardSequence.Join(
            _messageCardCanvasGroup
                .DOFade(1f, Mathf.Max(0f, _openDuration))
                .SetEase(Ease.OutQuad)
                .SetUpdate(true));
        _messageCardSequence.Join(
            _messageCardTransform
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
        KillMessageCardTweens();

        if (_messageCardTransform == null || _messageCardCanvasGroup == null)
        {
            callback?.Invoke();
            return;
        }

        _messageCardSequence = DOTween.Sequence()
            .SetTarget(_messageCardTransform)
            .SetUpdate(true);
        _messageCardSequence.Join(
            _messageCardCanvasGroup
                .DOFade(0f, Mathf.Max(0f, _closeDuration))
                .SetEase(Ease.InQuad)
                .SetUpdate(true));
        _messageCardSequence.Join(
            _messageCardTransform
                .DOScale(0.92f, Mathf.Max(0f, _closeDuration))
                .SetEase(_closeEase)
                .SetUpdate(true));
        _messageCardSequence.OnComplete(() =>
        {
            _messageCardSequence = null;
            callback?.Invoke();
        });
    }

    private void PositionMessageCard(RectTransform target, bool forceCenteredPosition)
    {
        if (_messageCardTransform == null || _canvasRect == null)
            return;

        Vector2 position;
        if (forceCenteredPosition)
        {
            position = Vector2.zero;
        }
        else if (target != null)
        {
            position = CalculateContextualMessagePosition(target);
        }
        else
        {
            position = ClampMessagePosition(_defaultMessagePosition);
        }

        _messageCardTransform.anchoredPosition = position;
    }

    private Vector2 CalculateContextualMessagePosition(RectTransform target)
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
            CalculatePositionForSide(primarySide, targetMin, targetMax));
        if (!DoesMessageOverlapTarget(primaryPosition, targetMin, targetMax))
            return primaryPosition;

        Vector2 secondaryPosition = ClampMessagePosition(
            CalculatePositionForSide(secondarySide, targetMin, targetMax));
        if (!DoesMessageOverlapTarget(secondaryPosition, targetMin, targetMax))
            return secondaryPosition;

        return primaryPosition;
    }

    private Vector2 CalculatePositionForSide(MessageSide side, Vector2 targetMin, Vector2 targetMax)
    {
        Rect cardRect = _messageCardTransform.rect;
        Vector2 pivot = _messageCardTransform.pivot;
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

    private Vector2 ClampMessagePosition(Vector2 position)
    {
        Rect canvasBounds = _canvasRect.rect;
        Rect cardRect = _messageCardTransform.rect;
        Vector2 pivot = _messageCardTransform.pivot;

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
        Rect cardRect = _messageCardTransform.rect;
        Vector2 pivot = _messageCardTransform.pivot;
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
            _messageCardTransform.anchoredPosition = ClampMessagePosition(position);
            _messageCardTransform.localScale = Vector3.one * 0.92f;
        }

        if (_messageCardCanvasGroup != null)
            _messageCardCanvasGroup.alpha = 0f;
    }

    private enum MessageSide
    {
        Above,
        Below,
        Left,
        Right
    }
}
