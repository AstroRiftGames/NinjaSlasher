using UnityEngine;

public class ComboVisualFeedback : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private ComboFeedbackConfig config;

    [Header("Pool")]
    [SerializeField] private FloatingTextPool textPool;

    [Header("Canvas Target")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private bool autoFindCanvas = true;

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnEnable()
    {
        GameEvents.OnComboUpdated += HandleComboUpdated;
    }

    private void OnDisable()
    {
        GameEvents.OnComboUpdated -= HandleComboUpdated;
    }

    private void Start()
    {
        if (autoFindCanvas && targetCanvas == null)
        {
            targetCanvas = FindGameplayCanvas();
        }
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

    private Canvas FindGameplayCanvas()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);

        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.name.Contains("Gameplay") || canvas.name.Contains("gameplay"))
            {
                Debug.Log($"[ComboVisualFeedback] Canvas de gameplay encontrado: {canvas.name}");
                return canvas;
            }
        }

        Canvas fallbackCanvas = FindObjectOfType<Canvas>();
        if (fallbackCanvas != null)
        {
            Debug.LogWarning($"[ComboVisualFeedback] Usando canvas por defecto: {fallbackCanvas.name}");
        }

        return fallbackCanvas;
    }

    private void HandleComboUpdated(int comboLevel, Vector3 enemyPosition)
    {
        if (!config.ShouldShowFeedback(comboLevel))
        {
            return;
        }

        ComboLevelData data = config.GetComboData(comboLevel);

        if (data == null)
        {
            Debug.LogWarning($"[ComboVisualFeedback] No hay datos para combo nivel {comboLevel}");
            return;
        }

        ShowFloatingText(data.message, enemyPosition, data.color);
    }

    private void ShowFloatingText(string message, Vector3 worldPosition, Color color)
    {
        if (textPool == null)
        {
            Debug.LogError("[ComboVisualFeedback] Pool no disponible");
            return;
        }

        FloatingComboText text = textPool.Get();

        if (text == null)
        {
            Debug.LogError("[ComboVisualFeedback] No se pudo obtener texto del pool");
            return;
        }

        // Asegurar que el texto esté bajo el canvas correcto para que GetComponentInParent<Canvas>() funcione
        if (targetCanvas != null && text.transform.parent != targetCanvas.transform)
            text.transform.SetParent(targetCanvas.transform, false);

        text.Show(message, worldPosition, color);
    }
}