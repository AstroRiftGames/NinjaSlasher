using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class KatanaSlashTransitionPreview : MonoBehaviour
{
    private const string BackdropName = "DemoBackdrop";
    private const string OverlayName = "SlashOverlay";
    private const float MaxAperture = 2.35f;

    [Header("References")]
    [SerializeField] private Material _baseMaterial;

    [Header("Playback")]
    [SerializeField] private bool _autoPlayOnStart = true;
    [SerializeField] private float _fadeToBlackDuration = 0.4f;
    [SerializeField] private float _delayBeforeSlash = 0.11f;
    [SerializeField] private float _slashTravelDuration = 0.56f;
    [SerializeField] private float _postSlashTrailDuration = 0.08f;
    [SerializeField] private float _delayAfterSlash = 0f;
    [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
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
    [SerializeField, Range(0f, 0.6f)] private float _openFollowDelay = 0.08f;
    [SerializeField, Min(0f)] private float _openWidthBehindSlash = 1.1f;

    [Header("Slash Look")]
    [SerializeField, Min(0.0005f)] private float _edgeSoftness = 0.019f;
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

    [Header("Cut Curvature")]
    [SerializeField, Min(0f)] private float _cutCurvatureAmount = 0.045f;
    [SerializeField, Range(0f, 1f)] private float _cutCurvatureBias = 0.58f;
    [SerializeField, Min(0.01f)] private float _cutCurvatureFalloff = 1.8f;

    [Header("Cut Sharpness")]
    [SerializeField, Min(0.05f)] private float _cutBodyWidthScale = 1f;
    [SerializeField, Range(0.05f, 1f)] private float _cutTipTaper = 0.2f;
    [SerializeField, Range(0.05f, 1f)] private float _cutTailTaper = 0.45f;
    [SerializeField, Min(0.01f)] private float _cutTaperSharpness = 1.35f;

    [Header("Sandbox")]
    [SerializeField] private bool _showDemoBackdrop = true;
    [SerializeField] private Color _backdropColor = new Color(0.11f, 0.13f, 0.16f, 1f);
    [SerializeField] private int _sortingOrder = 5000;

    [Header("Editor Preview")]
    [SerializeField, Range(0f, 1f)] private float _editorBlackAmount;
    [SerializeField, Range(0f, 1f)] private float _editorSlashAmount;

    private Canvas _canvas;
    private CanvasScaler _canvasScaler;
    private GraphicRaycaster _graphicRaycaster;
    private Image _demoBackdrop;
    private RawImage _slashOverlay;
    private Material _runtimeMaterial;
    private Coroutine _previewRoutine;
    private float _currentBlackAmount;
    private float _currentSlashTravel;

    public float FadeToBlackDuration => _fadeToBlackDuration;
    public float SlashTravelDuration => _slashTravelDuration;
    public float CutAngle => _cutAngle;
    public float CutPosition => _cutPosition;
    public float EdgeSoftness => _edgeSoftness;
    public float SlashLineThickness => _slashLineThickness;
    public float DelayBeforeSlash => _delayBeforeSlash;
    public float DelayAfterSlash => _delayAfterSlash;

    private void Reset()
    {
        EnsureSetup();
        SetEditorPose(0f, 0f);
    }

    private void Awake()
    {
        EnsureSetup();
        ApplyCurrentPose();
    }

    private void OnEnable()
    {
        EnsureSetup();
        ApplyCurrentPose();

        if (Application.isPlaying && _autoPlayOnStart)
        {
            PlayPreview();
        }
    }

    private void OnDisable()
    {
        StopPreview();
        DestroyRuntimeMaterial();
    }

    private void OnValidate()
    {
        EnsureSetup();

        _fadeToBlackDuration = Mathf.Max(0.01f, _fadeToBlackDuration);
        _slashTravelDuration = Mathf.Max(0.01f, _slashTravelDuration);
        _travelOverscan = Mathf.Max(1.5f, _travelOverscan);
        _slashFrontWidth = Mathf.Max(0.0001f, _slashFrontWidth);
        _trailLength = Mathf.Max(0f, _trailLength);
        _openWidthBehindSlash = Mathf.Max(0f, _openWidthBehindSlash);
        _edgeSoftness = Mathf.Max(0.0005f, _edgeSoftness);
        _slashLineThickness = Mathf.Max(0f, _slashLineThickness);
        _coreLineIntensity = Mathf.Max(0f, _coreLineIntensity);
        _glowThickness = Mathf.Max(0f, _glowThickness);
        _sweepIntensity = Mathf.Max(0f, _sweepIntensity);
        _lineIrregularity = Mathf.Max(0f, _lineIrregularity);
        _irregularityFrequency = Mathf.Max(0f, _irregularityFrequency);
        _postTraceIntensity = Mathf.Max(0f, _postTraceIntensity);
        _cutCurvatureAmount = Mathf.Max(0f, _cutCurvatureAmount);
        _cutCurvatureFalloff = Mathf.Max(0.01f, _cutCurvatureFalloff);
        _cutBodyWidthScale = Mathf.Max(0.05f, _cutBodyWidthScale);
        _cutTaperSharpness = Mathf.Max(0.01f, _cutTaperSharpness);
        _sortingOrder = Mathf.Max(0, _sortingOrder);

        if (Application.isPlaying)
        {
            ApplyCurrentPose();
            return;
        }

        SetEditorPose(_editorBlackAmount, _editorSlashAmount);
    }

    [ContextMenu("Play Preview")]
    public void PlayPreview()
    {
        if (!Application.isPlaying)
        {
            SetEditorPose(1f, 0.5f);
            return;
        }

        StopPreview();
        _previewRoutine = StartCoroutine(PlayPreviewRoutine());
    }

    [ContextMenu("Stop Preview")]
    public void StopPreview()
    {
        if (_previewRoutine != null)
        {
            StopCoroutine(_previewRoutine);
            _previewRoutine = null;
        }
    }

    [ContextMenu("Preview Closed")]
    public void PreviewClosed()
    {
        StopPreview();
        SetEditorPose(0f, 0f);
    }

    [ContextMenu("Preview Mid Slash")]
    public void PreviewMidSlash()
    {
        StopPreview();
        SetEditorPose(1f, 0.45f);
    }

    [ContextMenu("Preview Open")]
    public void PreviewOpen()
    {
        StopPreview();
        SetEditorPose(1f, 1f);
    }

    [ContextMenu("Rebuild Sandbox")]
    public void RebuildSandbox()
    {
        EnsureSetup(forceMaterialRefresh: true);
        ApplyCurrentPose();
    }

    private IEnumerator PlayPreviewRoutine()
    {
        SetRuntimePose(0f, 0f);
        yield return AnimatePhase(0f, 1f, _fadeToBlackDuration, _fadeCurve, blackOnly: true);

        if (_delayBeforeSlash > 0f)
        {
            yield return new WaitForSecondsRealtime(_delayBeforeSlash);
        }

        SetRuntimePose(1f, 0f);
        yield return AnimatePhase(0f, 1f, _slashTravelDuration, _slashTravelCurve, blackOnly: false);

        if (_postSlashTrailDuration > 0f)
        {
            yield return AnimatePostSlashTrail();
        }

        if (_delayAfterSlash > 0f)
        {
            yield return new WaitForSecondsRealtime(_delayAfterSlash);
        }

        _previewRoutine = null;
    }

    private IEnumerator AnimatePhase(float from, float to, float duration, AnimationCurve curve, bool blackOnly)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float eased = curve != null ? curve.Evaluate(normalized) : normalized;
            float value = Mathf.LerpUnclamped(from, to, eased);

            if (blackOnly)
            {
                SetRuntimePose(value, 0f);
            }
            else
            {
                SetRuntimePose(1f, value);
            }

            yield return null;
        }

        if (blackOnly)
        {
            SetRuntimePose(to, 0f);
        }
        else
        {
            SetRuntimePose(1f, to);
        }
    }

    private IEnumerator AnimatePostSlashTrail()
    {
        float elapsed = 0f;

        while (elapsed < _postSlashTrailDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / _postSlashTrailDuration);
            float eased = 1f - normalized;
            ApplyMaterialProperties(1f, 1f, eased);
            yield return null;
        }

        ApplyMaterialProperties(1f, 1f, 0f);
    }

    private void SetRuntimePose(float blackAmount, float slashTravel)
    {
        _currentBlackAmount = Mathf.Clamp01(blackAmount);
        _currentSlashTravel = Mathf.Clamp01(slashTravel);
        ApplyMaterialProperties(_currentBlackAmount, _currentSlashTravel, 1f);
    }

    private void SetEditorPose(float blackAmount, float slashTravel)
    {
        _editorBlackAmount = Mathf.Clamp01(blackAmount);
        _editorSlashAmount = Mathf.Clamp01(slashTravel);
        ApplyMaterialProperties(_editorBlackAmount, _editorSlashAmount, Mathf.Clamp01(_editorSlashAmount));
    }

    private void ApplyCurrentPose()
    {
        if (Application.isPlaying)
        {
            ApplyMaterialProperties(_currentBlackAmount, _currentSlashTravel, 1f);
            return;
        }

        ApplyMaterialProperties(_editorBlackAmount, _editorSlashAmount, Mathf.Clamp01(_editorSlashAmount));
    }

    private void ApplyMaterialProperties(float blackAmount, float slashTravel, float energyAmount)
    {
        EnsureSetup();

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
        _runtimeMaterial.SetFloat("_LineThickness", _slashLineThickness);
        _runtimeMaterial.SetFloat("_CoreLineIntensity", _coreLineIntensity);
        _runtimeMaterial.SetFloat("_GlowThickness", _glowThickness);
        _runtimeMaterial.SetFloat("_LineIrregularity", _lineIrregularity);
        _runtimeMaterial.SetFloat("_IrregularityFrequency", _irregularityFrequency);
        _runtimeMaterial.SetFloat("_SweepIntensity", _sweepIntensity);
        _runtimeMaterial.SetFloat("_Energy", Mathf.Clamp01(energyAmount));
        _runtimeMaterial.SetFloat("_PostTraceIntensity", _postTraceIntensity);
        _runtimeMaterial.SetFloat("_MaxAperture", MaxAperture);
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

        if (_canvas != null)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = _sortingOrder;
        }

        if (_demoBackdrop != null)
        {
            _demoBackdrop.enabled = _showDemoBackdrop;
            _demoBackdrop.color = _backdropColor;
        }
    }

    private float EvaluateOpenProgress(float travel)
    {
        float delay = Mathf.Clamp(_openFollowDelay, 0f, 0.95f);
        if (delay <= 0f)
        {
            return Mathf.Clamp01(travel);
        }

        return Mathf.Clamp01((travel - delay) / (1f - delay));
    }

    private void EnsureSetup(bool forceMaterialRefresh = false)
    {
        EnsureRootComponents();
        EnsureBackdrop();
        EnsureOverlay(forceMaterialRefresh);
    }

    private void EnsureRootComponents()
    {
        if (!_canvas && !TryGetComponent(out _canvas))
        {
            _canvas = gameObject.AddComponent<Canvas>();
        }

        if (!_canvasScaler && !TryGetComponent(out _canvasScaler))
        {
            _canvasScaler = gameObject.AddComponent<CanvasScaler>();
        }

        if (!_graphicRaycaster && !TryGetComponent(out _graphicRaycaster))
        {
            _graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
        }

        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.pixelPerfect = false;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = _sortingOrder;

        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        _canvasScaler.matchWidthOrHeight = 0.5f;
    }

    private void EnsureBackdrop()
    {
        RectTransform rectTransform = GetOrCreateChild(BackdropName);

        if (!_demoBackdrop && !rectTransform.TryGetComponent(out _demoBackdrop))
        {
            _demoBackdrop = rectTransform.gameObject.AddComponent<Image>();
        }

        StretchFullScreen(rectTransform);
        _demoBackdrop.raycastTarget = false;
        _demoBackdrop.color = _backdropColor;
    }

    private void EnsureOverlay(bool forceMaterialRefresh)
    {
        RectTransform rectTransform = GetOrCreateChild(OverlayName);

        if (!_slashOverlay && !rectTransform.TryGetComponent(out _slashOverlay))
        {
            _slashOverlay = rectTransform.gameObject.AddComponent<RawImage>();
        }

        StretchFullScreen(rectTransform);
        _slashOverlay.raycastTarget = false;

        if (forceMaterialRefresh || _runtimeMaterial == null || _slashOverlay.material != _runtimeMaterial)
        {
            RecreateRuntimeMaterial();
        }
    }

    private RectTransform GetOrCreateChild(string childName)
    {
        Transform existing = transform.Find(childName);
        RectTransform rectTransform;

        if (existing != null)
        {
            rectTransform = existing as RectTransform;
            if (rectTransform != null)
            {
                return rectTransform;
            }

            DestroyImmediate(existing.gameObject);
        }

        GameObject child = new GameObject(childName, typeof(RectTransform));
        child.transform.SetParent(transform, false);
        rectTransform = child.GetComponent<RectTransform>();
        return rectTransform;
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

    private void RecreateRuntimeMaterial()
    {
        DestroyRuntimeMaterial();

        Material sourceMaterial = _baseMaterial;
        if (sourceMaterial == null)
        {
            Shader shader = Shader.Find("UI/Katana Slash Transition Preview");
            if (shader != null)
            {
                sourceMaterial = new Material(shader);
            }
        }

        if (sourceMaterial == null)
        {
            return;
        }

        _runtimeMaterial = new Material(sourceMaterial)
        {
            name = $"{sourceMaterial.name} (Preview Instance)",
            hideFlags = HideFlags.HideAndDontSave
        };

        if (_slashOverlay != null)
        {
            _slashOverlay.material = _runtimeMaterial;
            _slashOverlay.color = Color.white;
        }
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
}
