using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class KatanaSlashTransitionOverlay : MonoBehaviour
{
    private const string OverlayRootName = "KatanaTransitionOverlay";
    private const string OverlayImageName = "KatanaTransitionImage";
    private const string DefaultMaterialResourcePath = "Systems/M_KatanaSlashTransition";
    private const string DefaultShaderName = "UI/Katana Slash Transition";
    private const float MaxAperture = 2.35f;

    [Header("References")]
    [SerializeField] private Material _baseMaterial;
    [SerializeField] private RawImage _slashOverlayImage;

    [Header("Playback")]
    [SerializeField] private float _coverFadeDuration = 0.4f;
    [SerializeField] private float _delayBeforeReveal = 2f;
    [SerializeField] private float _slashTravelDuration = 0.12f;
    [SerializeField] private float _postSlashTrailDuration = 0.08f;
    [SerializeField] private float _delayAfterReveal = 0f;
    [SerializeField] private AnimationCurve _coverFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve _slashTravelCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 3.6f),
        new Keyframe(0.22f, 0.58f, 2.1f, 0.9f),
        new Keyframe(1f, 1f, 0.18f, 0f));

    [Header("Slash Path")]
    [SerializeField, Range(-180f, 180f)] private float _slashTravelDirection = 45f;
    [SerializeField, Range(-180f, 180f)] private float _cutAngle = -45f;
    [SerializeField, Range(-1.25f, 1.25f)] private float _cutPosition = 0f;
    [SerializeField, Min(1.5f)] private float _travelOverscan = 2.2f;
    [SerializeField, Min(0f)] private float _slashFrontWidth = 0.055f;
    [SerializeField, Min(0f)] private float _trailLength = 0.22f;
    [SerializeField, Range(0f, 0.6f)] private float _openFollowDelay = 0.4f;
    [SerializeField, Min(0f)] private float _openWidthBehindSlash = 1.1f;

    [Header("Slash Look")]
    [SerializeField, Min(0.0005f)] private float _edgeSoftness = 0.019f;
    [SerializeField, Min(0.00025f)] private float _cutFeather = 0.0025f;
    [SerializeField, Min(0f)] private float _slashLineThickness = 0.011f;
    [SerializeField, Min(0f)] private float _coreLineIntensity = 1.2f;
    [SerializeField, Min(0f)] private float _glowThickness = 0.045f;
    [SerializeField, Min(0f)] private float _sweepIntensity = 0.72f;
    [SerializeField, Min(0f)] private float _lineIrregularity = 0.002f;
    [SerializeField, Min(0f)] private float _irregularityFrequency = 8f;
    [SerializeField] private Color _highlightColor = new Color(1f, 0.96f, 0.82f, 0.9f);
    [SerializeField] private Color _coreLineColor = new Color(1f, 1f, 1f, 1f);

    [Header("Afterglow")]
    [SerializeField, Min(0f)] private float _postTraceIntensity = 0.22f;

    [Header("Split Motion")]
    [SerializeField, Min(0f)] private float _halfSeparation = 1.7f;
    [SerializeField, Min(0f)] private float _halfDrop = 0.18f;
    [SerializeField, Min(0.01f)] private float _halfDropExponent = 1.45f;

    [Header("Cut Curvature")]
    [SerializeField, Min(0f)] private float _cutCurvatureAmount = 0.045f;
    [SerializeField, Range(0f, 1f)] private float _cutCurvatureBias = 0.58f;
    [SerializeField, Min(0.01f)] private float _cutCurvatureFalloff = 1.8f;

    [Header("Cut Sharpness")]
    [SerializeField, Min(0.05f)] private float _cutBodyWidthScale = 1f;
    [SerializeField, Range(0.05f, 1f)] private float _cutTipTaper = 0.2f;
    [SerializeField, Range(0.05f, 1f)] private float _cutTailTaper = 0.45f;
    [SerializeField, Min(0.01f)] private float _cutTaperSharpness = 1.35f;

    [Header("Overlay")]
    [SerializeField] private int _sortingOrder = 32000;
    [SerializeField] private bool _blockRaycastsWhilePlaying = true;

    private GameObject _overlayRoot;
    private Canvas _overlayCanvas;
    private CanvasScaler _overlayCanvasScaler;
    private GraphicRaycaster _overlayGraphicRaycaster;
    private Image _blackOverlayImage;
    private Material _runtimeMaterial;
    private Coroutine _transitionRoutine;

    public bool IsTransitionPlaying => _transitionRoutine != null;
    public bool CanPlay
    {
        get
        {
            EnsureOverlaySetup();
            return _runtimeMaterial != null;
        }
    }

    private void Awake()
    {
        EnsureOverlaySetup();
        ResetToClosedState();
    }

    private void OnDestroy()
    {
        DestroyRuntimeMaterial();
    }

    private void OnValidate()
    {
        _coverFadeDuration = Mathf.Max(0.01f, _coverFadeDuration);
        _delayBeforeReveal = Mathf.Max(0f, _delayBeforeReveal);
        _slashTravelDuration = Mathf.Max(0.01f, _slashTravelDuration);
        _postSlashTrailDuration = Mathf.Max(0f, _postSlashTrailDuration);
        _delayAfterReveal = Mathf.Max(0f, _delayAfterReveal);
        _travelOverscan = Mathf.Max(1.5f, _travelOverscan);
        _slashFrontWidth = Mathf.Max(0.0001f, _slashFrontWidth);
        _trailLength = Mathf.Max(0f, _trailLength);
        _openWidthBehindSlash = Mathf.Max(0f, _openWidthBehindSlash);
        _edgeSoftness = Mathf.Max(0.0005f, _edgeSoftness);
        _cutFeather = Mathf.Max(0.00025f, _cutFeather);
        _slashLineThickness = Mathf.Max(0f, _slashLineThickness);
        _coreLineIntensity = Mathf.Max(0f, _coreLineIntensity);
        _glowThickness = Mathf.Max(0f, _glowThickness);
        _sweepIntensity = Mathf.Max(0f, _sweepIntensity);
        _lineIrregularity = Mathf.Max(0f, _lineIrregularity);
        _irregularityFrequency = Mathf.Max(0f, _irregularityFrequency);
        _postTraceIntensity = Mathf.Max(0f, _postTraceIntensity);
        _halfSeparation = Mathf.Max(0f, _halfSeparation);
        _halfDrop = Mathf.Max(0f, _halfDrop);
        _halfDropExponent = Mathf.Max(0.01f, _halfDropExponent);
        _cutCurvatureAmount = Mathf.Max(0f, _cutCurvatureAmount);
        _cutCurvatureFalloff = Mathf.Max(0.01f, _cutCurvatureFalloff);
        _cutBodyWidthScale = Mathf.Max(0.05f, _cutBodyWidthScale);
        _cutTaperSharpness = Mathf.Max(0.01f, _cutTaperSharpness);
        _sortingOrder = Mathf.Max(0, _sortingOrder);
    }

    public Coroutine PlayTransition(Action onCovered = null, Action onRevealStarted = null, Action onCompleted = null)
    {
        return PlayFadeToBlackThenReveal(onCovered, onRevealStarted, onCompleted);
    }

    public Coroutine PlayTransition(Func<IEnumerator> coveredSequence, Action onRevealStarted = null, Action onCompleted = null)
    {
        return PlayFadeToBlackThenReveal(coveredSequence, onRevealStarted, onCompleted);
    }

    public Coroutine PlayTransitionAndLoadScene(
        string sceneName,
        LoadSceneMode loadMode = LoadSceneMode.Single,
        Action onSceneLoaded = null,
        Action onRevealStarted = null,
        Action onCompleted = null)
    {
        return PlayFadeToBlackThenLoadScene(sceneName, loadMode, onSceneLoaded, onRevealStarted, onCompleted);
    }

    public Coroutine PlayFadeToBlackThenReveal(Action onCovered = null, Action onRevealStarted = null, Action onCompleted = null)
    {
        return StartTransition(onCovered, null, null, onRevealStarted, onCompleted);
    }

    public Coroutine PlayFadeToBlackThenReveal(Func<IEnumerator> coveredSequence, Action onRevealStarted = null, Action onCompleted = null)
    {
        return StartTransition(null, coveredSequence, null, onRevealStarted, onCompleted);
    }

    public Coroutine PlayFadeToBlackThenLoadScene(
        string sceneName,
        LoadSceneMode loadMode = LoadSceneMode.Single,
        Action onSceneLoaded = null,
        Action onRevealStarted = null,
        Action onCompleted = null)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[KatanaSlashTransitionOverlay] Scene name is empty.");
            return null;
        }

        return StartTransition(
            null,
            () => LoadSceneWhileCovered(sceneName, loadMode, onSceneLoaded),
            null,
            onRevealStarted,
            onCompleted);
    }

    public Task PlayTransitionAsync(Func<Task> coveredOperationAsync = null, Action onRevealStarted = null)
    {
        return PlayFadeToBlackThenRevealAsync(coveredOperationAsync, onRevealStarted);
    }

    public Task PlayFadeToBlackThenRevealAsync(Func<Task> coveredOperationAsync = null, Action onRevealStarted = null)
    {
        TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
        Coroutine routine = StartTransition(null, null, coveredOperationAsync, onRevealStarted, () => completionSource.TrySetResult(true));

        if (routine == null)
        {
            completionSource.TrySetException(new InvalidOperationException("Transition is already playing or the overlay is unavailable."));
        }

        return completionSource.Task;
    }

    private Coroutine StartTransition(
        Action onCovered,
        Func<IEnumerator> coveredSequence,
        Func<Task> coveredTaskFactory,
        Action onRevealStarted,
        Action onCompleted)
    {
        if (IsTransitionPlaying)
        {
            Debug.LogWarning("[KatanaSlashTransitionOverlay] Transition request ignored because another transition is already playing.");
            return null;
        }

        EnsureOverlaySetup();
        if (_runtimeMaterial == null)
        {
            Debug.LogWarning("[KatanaSlashTransitionOverlay] Transition request ignored because no runtime material is available.");
            return null;
        }

        _transitionRoutine = StartCoroutine(PlayTransitionRoutine(onCovered, coveredSequence, coveredTaskFactory, onRevealStarted, onCompleted));
        return _transitionRoutine;
    }

    private IEnumerator PlayTransitionRoutine(
        Action onCovered,
        Func<IEnumerator> coveredSequence,
        Func<Task> coveredTaskFactory,
        Action onRevealStarted,
        Action onCompleted)
    {
        SetOverlayActive(true);
        SetBlackOverlayAlpha(0f);
        SetSlashOverlayVisible(false);
        ApplyMaterialProperties(1f, 0f, 0f);

        // Phase 1: cover the screen with a plain black fade.
        yield return AnimateCoverToBlack();
        TryInvoke(onCovered, "covered callback");

        if (coveredSequence != null)
        {
            IEnumerator sequence = null;

            try
            {
                sequence = coveredSequence.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            if (sequence != null)
            {
                yield return StartCoroutine(sequence);
            }
        }

        if (coveredTaskFactory != null)
        {
            Task task = null;

            try
            {
                task = coveredTaskFactory.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            if (task != null)
            {
                yield return WaitForTask(task);
            }
        }

        if (_delayBeforeReveal > 0f)
        {
            yield return WaitForSecondsRealtime(_delayBeforeReveal);
        }

        // Phase 2: swap from the solid black overlay to the katana overlay on the same black frame.
        ApplyMaterialProperties(1f, 0f, 1f);
        SetSlashOverlayVisible(true);
        TryInvoke(onRevealStarted, "reveal callback");

        yield return AnimateRevealSlash();

        SetBlackOverlayAlpha(0f);

        if (_postSlashTrailDuration > 0f)
        {
            yield return AnimatePostSlashTrail();
        }

        if (_delayAfterReveal > 0f)
        {
            yield return WaitForSecondsRealtime(_delayAfterReveal);
        }

        ResetToClosedState();
        _transitionRoutine = null;
        TryInvoke(onCompleted, "completion callback");
    }

    private IEnumerator AnimateCoverToBlack()
    {
        if (_coverFadeDuration <= 0f)
        {
            SetBlackOverlayAlpha(1f);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < _coverFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / _coverFadeDuration);
            float eased = _coverFadeCurve != null ? _coverFadeCurve.Evaluate(normalized) : normalized;
            SetBlackOverlayAlpha(Mathf.LerpUnclamped(0f, 1f, eased));
            yield return null;
        }

        SetBlackOverlayAlpha(1f);
    }

    private IEnumerator AnimateRevealSlash()
    {
        ApplyMaterialProperties(1f, 0f, 1f);
        yield return AnimateValue(0f, 1f, _slashTravelDuration, _slashTravelCurve, false);
    }

    private IEnumerator AnimatePostSlashTrail()
    {
        float elapsed = 0f;

        while (elapsed < _postSlashTrailDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / _postSlashTrailDuration);
            ApplyMaterialProperties(1f, 1f, 1f - normalized);
            yield return null;
        }

        ApplyMaterialProperties(1f, 1f, 0f);
    }

    private IEnumerator AnimateValue(float from, float to, float duration, AnimationCurve curve, bool blackOnly)
    {
        if (duration <= 0f)
        {
            if (blackOnly)
            {
                ApplyMaterialProperties(to, 0f, 0f);
            }
            else
            {
                ApplyMaterialProperties(1f, to, 1f);
            }

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float eased = curve != null ? curve.Evaluate(normalized) : normalized;
            float value = Mathf.LerpUnclamped(from, to, eased);

            if (blackOnly)
            {
                ApplyMaterialProperties(value, 0f, 0f);
            }
            else
            {
                ApplyMaterialProperties(1f, value, 1f);
            }

            yield return null;
        }

        if (blackOnly)
        {
            ApplyMaterialProperties(to, 0f, 0f);
        }
        else
        {
            ApplyMaterialProperties(1f, to, 1f);
        }
    }

    private IEnumerator LoadSceneWhileCovered(string sceneName, LoadSceneMode loadMode, Action onSceneLoaded)
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, loadMode);
        if (loadOperation == null)
        {
            yield break;
        }

        loadOperation.allowSceneActivation = false;

        while (loadOperation.progress < 0.9f)
        {
            yield return null;
        }

        loadOperation.allowSceneActivation = true;

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return null;
        TryInvoke(onSceneLoaded, "scene loaded callback");
    }

    private IEnumerator WaitForTask(Task task)
    {
        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.IsFaulted)
        {
            Debug.LogException(task.Exception);
        }
    }

    private IEnumerator WaitForSecondsRealtime(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void ResetToClosedState()
    {
        SetBlackOverlayAlpha(0f);
        SetSlashOverlayVisible(false);
        ApplyMaterialProperties(1f, 0f, 0f);
        SetOverlayActive(false);
    }

    private void ApplyMaterialProperties(float blackAmount, float slashTravel, float energyAmount)
    {
        EnsureOverlaySetup();
        if (_runtimeMaterial == null)
        {
            return;
        }

        _runtimeMaterial.SetFloat("_Opacity", Mathf.Clamp01(blackAmount));
        _runtimeMaterial.SetFloat("_Travel", Mathf.Clamp01(slashTravel));
        _runtimeMaterial.SetFloat("_TravelPosition", Mathf.Lerp(_travelOverscan, -_travelOverscan, Mathf.Clamp01(slashTravel)));
        _runtimeMaterial.SetFloat("_OpenProgress", EvaluateOpenProgress(slashTravel));
        _runtimeMaterial.SetFloat("_TravelDirection", _slashTravelDirection);
        _runtimeMaterial.SetFloat("_Angle", _cutAngle);
        _runtimeMaterial.SetFloat("_CutPosition", _cutPosition);
        _runtimeMaterial.SetFloat("_FrontWidth", _slashFrontWidth);
        _runtimeMaterial.SetFloat("_TrailLength", _trailLength);
        _runtimeMaterial.SetFloat("_OpenWidth", _openWidthBehindSlash);
        _runtimeMaterial.SetFloat("_EdgeSoftness", _edgeSoftness);
        _runtimeMaterial.SetFloat("_CutFeather", _cutFeather);
        _runtimeMaterial.SetFloat("_LineThickness", _slashLineThickness);
        _runtimeMaterial.SetFloat("_CoreLineIntensity", _coreLineIntensity);
        _runtimeMaterial.SetFloat("_GlowThickness", _glowThickness);
        _runtimeMaterial.SetFloat("_LineIrregularity", _lineIrregularity);
        _runtimeMaterial.SetFloat("_IrregularityFrequency", _irregularityFrequency);
        _runtimeMaterial.SetFloat("_SweepIntensity", _sweepIntensity);
        _runtimeMaterial.SetFloat("_Energy", Mathf.Clamp01(energyAmount));
        _runtimeMaterial.SetFloat("_PostTraceIntensity", _postTraceIntensity);
        _runtimeMaterial.SetFloat("_MaxAperture", MaxAperture);
        _runtimeMaterial.SetFloat("_HalfSeparation", _halfSeparation);
        _runtimeMaterial.SetFloat("_HalfDrop", _halfDrop);
        _runtimeMaterial.SetFloat("_HalfDropExponent", _halfDropExponent);
        _runtimeMaterial.SetFloat("_CutCurvatureAmount", _cutCurvatureAmount);
        _runtimeMaterial.SetFloat("_CutCurvatureBias", _cutCurvatureBias);
        _runtimeMaterial.SetFloat("_CutCurvatureFalloff", _cutCurvatureFalloff);
        _runtimeMaterial.SetFloat("_CutBodyWidthScale", _cutBodyWidthScale);
        _runtimeMaterial.SetFloat("_CutTipTaper", _cutTipTaper);
        _runtimeMaterial.SetFloat("_CutTailTaper", _cutTailTaper);
        _runtimeMaterial.SetFloat("_CutTaperSharpness", _cutTaperSharpness);
        _runtimeMaterial.SetColor("_TintColor", Color.black);
        _runtimeMaterial.SetColor("_HighlightColor", _highlightColor);
        _runtimeMaterial.SetColor("_CoreLineColor", _coreLineColor);
    }

    private float EvaluateOpenProgress(float travel)
    {
        float clampedDelay = Mathf.Clamp(_openFollowDelay, 0f, 0.95f);
        if (clampedDelay <= 0f)
        {
            return Mathf.Clamp01(travel);
        }

        float normalized = Mathf.Clamp01((travel - clampedDelay) / (1f - clampedDelay));
        return normalized * normalized;
    }

    private void EnsureOverlaySetup()
    {
        EnsureOverlayRoot();
        if (_slashOverlayImage == null)
        {
            EnsureOverlayComponents();
        }
        EnsureRuntimeMaterial();
    }

    private void EnsureOverlayRoot()
    {
        if (_overlayRoot != null)
        {
            return;
        }

        Transform existingRoot = transform.Find(OverlayRootName);
        if (existingRoot != null)
        {
            _overlayRoot = existingRoot.gameObject;
            return;
        }

        _overlayRoot = new GameObject(OverlayRootName, typeof(RectTransform));
        _overlayRoot.transform.SetParent(transform, false);
    }

    private void EnsureOverlayComponents()
    {
        if (_overlayRoot == null)
        {
            return;
        }

        if (!_overlayRoot.TryGetComponent(out _overlayCanvas))
        {
            _overlayCanvas = _overlayRoot.AddComponent<Canvas>();
        }

        if (!_overlayRoot.TryGetComponent(out _overlayCanvasScaler))
        {
            _overlayCanvasScaler = _overlayRoot.AddComponent<CanvasScaler>();
        }

        if (!_overlayRoot.TryGetComponent(out _overlayGraphicRaycaster))
        {
            _overlayGraphicRaycaster = _overlayRoot.AddComponent<GraphicRaycaster>();
        }

        _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _overlayCanvas.overrideSorting = true;
        _overlayCanvas.sortingOrder = _sortingOrder;
        _overlayCanvas.pixelPerfect = false;

        _overlayCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _overlayCanvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        _overlayCanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        _overlayCanvasScaler.matchWidthOrHeight = 0.5f;

        RectTransform blackTransform = GetOrCreateChildRect(_overlayRoot.transform, "BlackOverlay");
        StretchFullScreen(blackTransform);

        if (!blackTransform.TryGetComponent(out _blackOverlayImage))
        {
            _blackOverlayImage = blackTransform.gameObject.AddComponent<Image>();
        }

        _blackOverlayImage.color = Color.black;
        _blackOverlayImage.raycastTarget = false;
        blackTransform.SetAsLastSibling();

        RectTransform imageTransform = GetOrCreateChildRect(_overlayRoot.transform, OverlayImageName);
        StretchFullScreen(imageTransform);

        if (!imageTransform.TryGetComponent(out _slashOverlayImage))
        {
            _slashOverlayImage = imageTransform.gameObject.AddComponent<RawImage>();
        }

        _slashOverlayImage.color = Color.white;
        _slashOverlayImage.raycastTarget = false;
        imageTransform.SetAsFirstSibling();
    }

    private void EnsureRuntimeMaterial()
    {
        if (_runtimeMaterial != null)
        {
            if (_slashOverlayImage != null && _slashOverlayImage.material != _runtimeMaterial)
            {
                _slashOverlayImage.material = _runtimeMaterial;
            }

            return;
        }

        Material sourceMaterial = ResolveBaseMaterial();
        if (sourceMaterial == null)
        {
            Shader shader = Shader.Find(DefaultShaderName);
            if (shader != null)
            {
                sourceMaterial = new Material(shader);
            }
        }

        if (sourceMaterial == null)
        {
            return;
        }

        _runtimeMaterial = new Material(sourceMaterial);
        _runtimeMaterial.name = sourceMaterial.name + " (Runtime Instance)";

        if (_slashOverlayImage != null)
        {
            _slashOverlayImage.material = _runtimeMaterial;
        }
    }

    private Material ResolveBaseMaterial()
    {
        if (_baseMaterial != null)
        {
            return _baseMaterial;
        }

        _baseMaterial = Resources.Load<Material>(DefaultMaterialResourcePath);
        return _baseMaterial;
    }

    private void SetOverlayActive(bool active)
    {
        if (_overlayRoot == null)
        {
            return;
        }

        _overlayRoot.SetActive(active);

        if (_slashOverlayImage != null)
        {
            _slashOverlayImage.raycastTarget = active && _blockRaycastsWhilePlaying;
        }

        if (_blackOverlayImage != null)
        {
            _blackOverlayImage.raycastTarget = active && _blockRaycastsWhilePlaying;
        }
    }

    private void SetBlackOverlayAlpha(float alpha)
    {
        if (_blackOverlayImage == null)
        {
            return;
        }

        Color color = _blackOverlayImage.color;
        color.a = Mathf.Clamp01(alpha);
        _blackOverlayImage.color = color;
        _blackOverlayImage.enabled = color.a > 0.0001f;
    }

    private void SetSlashOverlayVisible(bool visible)
    {
        if (_slashOverlayImage == null)
        {
            return;
        }

        _slashOverlayImage.enabled = visible;
    }

    private void DestroyRuntimeMaterial()
    {
        if (_runtimeMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_runtimeMaterial);
        }
        else
        {
            DestroyImmediate(_runtimeMaterial);
        }

        _runtimeMaterial = null;
    }

    private static RectTransform GetOrCreateChildRect(Transform parent, string childName)
    {
        Transform existingChild = parent.Find(childName);
        if (existingChild != null)
        {
            RectTransform existingRect = existingChild as RectTransform;
            if (existingRect != null)
            {
                return existingRect;
            }

            if (Application.isPlaying)
            {
                Destroy(existingChild.gameObject);
            }
            else
            {
                DestroyImmediate(existingChild.gameObject);
            }
        }

        GameObject child = new GameObject(childName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static void StretchFullScreen(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }

    private static void TryInvoke(Action callback, string callbackName)
    {
        if (callback == null)
        {
            return;
        }

        try
        {
            callback.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogError("[KatanaSlashTransitionOverlay] Error while executing " + callbackName + ".");
            Debug.LogException(exception);
        }
    }
}
