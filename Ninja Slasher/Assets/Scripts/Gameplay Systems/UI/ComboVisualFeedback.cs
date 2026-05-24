using UnityEngine;

public class ComboVisualFeedback : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private ComboFeedbackConfig config;

    [Header("Pool")]
    [SerializeField] private FloatingTextPool textPool;

    [Header("Presentation")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform feedbackContainer;
    [SerializeField] private bool autoFindCanvas = true;

    private RectTransform _defaultFeedbackContainer;

    private void Awake()
    {
        _defaultFeedbackContainer = transform as RectTransform;
        ValidateReferences();
        ResolvePresentationTargets();
    }

    private void Start()
    {
        ResolvePresentationTargets();
    }

    private void OnEnable()
    {
        GameEvents.OnComboUpdated += HandleComboUpdated;
        GameEvents.OnLevelStarted += HandleLevelStarted;
        GameEvents.OnLevelResultReady += HandleLevelResultReady;
        GameEvents.OnLevelSessionClosed += HandleLevelSessionClosed;
    }

    private void OnDisable()
    {
        GameEvents.OnComboUpdated -= HandleComboUpdated;
        GameEvents.OnLevelStarted -= HandleLevelStarted;
        GameEvents.OnLevelResultReady -= HandleLevelResultReady;
        GameEvents.OnLevelSessionClosed -= HandleLevelSessionClosed;

        ClearActiveFeedbacks();
    }

    public void ClearActiveFeedbacks()
    {
        textPool?.ReleaseAllActive();
    }

    private void HandleLevelStarted()
    {
        ResolvePresentationTargets();
        ClearActiveFeedbacks();
    }

    private void HandleLevelResultReady(LevelResult _)
    {
        ClearActiveFeedbacks();
    }

    private void HandleLevelSessionClosed()
    {
        ClearActiveFeedbacks();
    }

    private void ValidateReferences()
    {
        if (config == null)
        {
            Debug.LogError("[ComboVisualFeedback] ComboFeedbackConfig no asignado", this);
        }

        if (textPool == null)
        {
            Debug.LogError("[ComboVisualFeedback] FloatingTextPool no asignado", this);
        }
    }

    private void HandleComboUpdated(int comboLevel, Vector3 enemyPosition)
    {
        if (!CanShowFeedback(comboLevel))
            return;

        ComboLevelData data = config.GetComboData(comboLevel);
        if (data == null)
        {
            Debug.LogWarning($"[ComboVisualFeedback] No hay datos para combo nivel {comboLevel}");
            return;
        }

        ShowFloatingText(data.message, enemyPosition, data.color);
    }

    private bool CanShowFeedback(int comboLevel)
    {
        if (config == null || textPool == null)
            return false;

        if (!config.ShouldShowFeedback(comboLevel))
            return false;

        if (!IsGameplaySessionRunning())
            return false;

        return ResolvePresentationTargets();
    }

    private void ShowFloatingText(string message, Vector3 worldPosition, Color color)
    {
        FloatingComboText text = textPool.Get();
        if (text == null)
        {
            Debug.LogError("[ComboVisualFeedback] No se pudo obtener texto del pool");
            return;
        }

        RectTransform container = GetFeedbackContainer();
        if (container == null || targetCanvas == null)
        {
            textPool.ReleaseAllActive();
            return;
        }

        if (text.transform.parent != container)
            text.transform.SetParent(container, false);

        text.Show(message, worldPosition, color, targetCanvas, container);
    }

    private bool ResolvePresentationTargets()
    {
        if (autoFindCanvas && targetCanvas == null)
        {
            targetCanvas = FindGameplayCanvas();
        }

        return targetCanvas != null && GetFeedbackContainer() != null;
    }

    private RectTransform GetFeedbackContainer()
    {
        if (feedbackContainer != null)
            return feedbackContainer;

        return _defaultFeedbackContainer;
    }

    private Canvas FindGameplayCanvas()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>(true);
        if (parentCanvas != null)
            return parentCanvas;

        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);

        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.name.Contains("Gameplay") || canvas.name.Contains("gameplay"))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[ComboVisualFeedback] Canvas de gameplay encontrado: {canvas.name}");
#endif
                return canvas;
            }
        }

        Canvas fallbackCanvas = FindObjectOfType<Canvas>(true);
        if (fallbackCanvas != null)
        {
            Debug.LogWarning($"[ComboVisualFeedback] Usando canvas por defecto: {fallbackCanvas.name}");
        }

        return fallbackCanvas;
    }

    private static bool IsGameplaySessionRunning()
    {
        return LevelSessionManager.Instance != null && LevelSessionManager.Instance.IsSessionRunning;
    }
}
