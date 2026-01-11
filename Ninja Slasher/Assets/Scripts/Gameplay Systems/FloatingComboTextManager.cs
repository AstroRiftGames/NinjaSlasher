using UnityEngine;
using System.Collections.Generic;
using System;

public class FloatingComboTextManager : MonoBehaviourSingleton<FloatingComboTextManager>
{
    [Header("REFERENCES")]
    [SerializeField] private FloatingComboText _floatingTextPrefab;
    [SerializeField] private Canvas _targetCanvas;
    [SerializeField] private int _poolSize = 10;

    [Header("SETTINGS")]
    [SerializeField]
    private ComboTextData[] _comboLevels = new ComboTextData[]
    {
        new ComboTextData { message = "COMBO x2!", color = new Color(1f, 1f, 0f) },
        new ComboTextData { message = "COMBO x3!!", color = new Color(1f, 0.5f, 0f) },
        new ComboTextData { message = "COMBO x4!!!", color = new Color(1f, 0.3f, 0f) },
        new ComboTextData { message = "COMBO x5!!!!", color = new Color(1f, 0f, 0f) }
    };

    private Queue<FloatingComboText> _textPool = new Queue<FloatingComboText>();
    private List<FloatingComboText> _activeTexts = new List<FloatingComboText>();

    public override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        InitializePool();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

        SubscribeToGameEvents();
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        UnsubscribeFromGameEvents();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        ReinitializePoolForNewScene();

        UnsubscribeFromGameEvents();
        SubscribeToGameEvents();

        Debug.Log($"[FloatingComboTextManager] Pool reinicializado para escena: {scene.name}");
    }

    private void Start()
    {
        if (_targetCanvas == null)
        {
            _targetCanvas = FindGameplayCanvas();
        }

        SubscribeToGameEvents();
    }

    private void SubscribeToGameEvents()
    {
        GameEvents.OnComboUpdated -= HandleComboUpdated;
        GameEvents.OnComboUpdated += HandleComboUpdated;

        Debug.Log("[FloatingComboTextManager] Suscrito a GameEvents.OnComboUpdated");
    }

    private void UnsubscribeFromGameEvents()
    {
        GameEvents.OnComboUpdated -= HandleComboUpdated;
    }

    private Canvas FindGameplayCanvas()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);

        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.name.Contains("Gameplay") || canvas.name.Contains("gameplay"))
            {
                Debug.Log($"[FloatingComboTextManager] Canvas de gameplay encontrado: {canvas.name}");
                return canvas;
            }
        }

        Canvas fallbackCanvas = FindObjectOfType<Canvas>();
        if (fallbackCanvas != null)
        {
            Debug.LogWarning($"[FloatingComboTextManager] Usando canvas por defecto: {fallbackCanvas.name}");
        }

        return fallbackCanvas;
    }

    private void InitializePool()
    {
        if (_floatingTextPrefab == null)
        {
            Debug.LogError("[FloatingComboTextManager] FloatingTextPrefab no asignado!");
            return;
        }

        if (_targetCanvas == null)
        {
            _targetCanvas = FindGameplayCanvas();

            if (_targetCanvas == null)
            {
                Debug.LogError("[FloatingComboTextManager] No se encontró canvas para el pool!");
                return;
            }
        }

        for (int i = 0; i < _poolSize; i++)
        {
            CreateNewText();
        }

        Debug.Log($"[FloatingComboTextManager] Pool inicializado con {_poolSize} textos");
    }

    private void ReinitializePoolForNewScene()
    {
        foreach (var text in _activeTexts.ToArray())
        {
            if (text != null)
            {
                Destroy(text.gameObject);
            }
        }
        _activeTexts.Clear();

        while (_textPool.Count > 0)
        {
            var text = _textPool.Dequeue();
            if (text != null)
            {
                Destroy(text.gameObject);
            }
        }

        if (_targetCanvas == null)
        {
            _targetCanvas = FindGameplayCanvas();
        }

        if (_targetCanvas != null)
        {
            InitializePool();
        }
    }

    private FloatingComboText CreateNewText()
    {
        FloatingComboText newText = Instantiate(_floatingTextPrefab, _targetCanvas.transform);
        newText.gameObject.SetActive(false);
        _textPool.Enqueue(newText);
        return newText;
    }

    private FloatingComboText GetFromPool()
    {
        if (_textPool.Count == 0)
        {
            Debug.LogWarning("[FloatingComboTextManager] Pool vacío, creando nuevo texto");
            return CreateNewText();
        }

        FloatingComboText text = _textPool.Dequeue();
        text.gameObject.SetActive(true);
        _activeTexts.Add(text);
        return text;
    }

    public void ReturnToPool(FloatingComboText text)
    {
        if (text == null) return;

        text.gameObject.SetActive(false);
        _activeTexts.Remove(text);
        _textPool.Enqueue(text);
    }

    private void HandleComboUpdated(int level, Vector3 enemyPosition)
    {
        if (level < 2)
        {
            return;
        }

        ComboTextData data = GetComboData(level);
        if (data == null)
        {
            Debug.LogWarning($"[FloatingComboTextManager] No hay datos para combo nivel {level}");
            return;
        }

        ShowFloatingText(data.message, enemyPosition, data.color);

        Debug.Log($"[FloatingComboTextManager] Mostrando '{data.message}' en {enemyPosition}");
    }

    private ComboTextData GetComboData(int level)
    {
        int index = Mathf.Clamp(level - 2, 0, _comboLevels.Length - 1);
        return _comboLevels[index];
    }

    public void ShowFloatingText(string message, Vector3 worldPosition, Color color)
    {
        FloatingComboText text = GetFromPool();

        if (text == null)
        {
            Debug.LogError("[FloatingComboTextManager] No se pudo obtener texto del pool");
            return;
        }

        text.Show(message, worldPosition, color);
    }

    [ContextMenu("Debug Pool State")]
    private void DebugPoolState()
    {
        Debug.Log($"[FloatingComboTextManager] Pool disponibles: {_textPool.Count}, Activos: {_activeTexts.Count}");
    }
}

[Serializable]
public class ComboTextData
{
    public string message = "COMBO!";
    public Color color = Color.white;
}