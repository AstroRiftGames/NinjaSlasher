using UnityEngine;
using System.Collections.Generic;
using System;

public class FloatingComboTextManager : MonoBehaviourSingleton<FloatingComboTextManager>
{
    [Header("REFERENCES")]
    [SerializeField] private FloatingComboText _floatingTextPrefab;
    [SerializeField] private Canvas _targetCanvas;

    //DEPRECATED
    //[SerializeField] private int _poolSize = 10;
    private int PoolSize => GameConfigManager.Config.floatingTextPoolSize;

    [Header("SETTINGS")]
    [SerializeField]
    private ComboTextData[] _comboLevels = new ComboTextData[]
    {
        new ComboTextData { message = "COMBO x2!", color = new Color(1f, 1f, 0f) },
        new ComboTextData { message = "COMBO x3!!", color = new Color(1f, 0.5f, 0f) },
        new ComboTextData { message = "COMBO x4!!!", color = new Color(1f, 0.3f, 0f) },
        new ComboTextData { message = "COMBO x5!!!!", color = new Color(1f, 0f, 0f) }
    };

    private GenericPool<FloatingComboText> _textPool;
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

        _textPool = new GenericPool<FloatingComboText>(
            _floatingTextPrefab.gameObject,
            PoolSize,
            _targetCanvas.transform,
            "FloatingComboTextPool"
        );

        _textPool.OnObjectRetrieved += (text) => {
            _activeTexts.Add(text);
        };

        _textPool.OnObjectReturned += (text) => {
            _activeTexts.Remove(text);
        };

        Debug.Log($"[FloatingComboTextManager] GenericPool inicializado con {PoolSize} textos");
    }

    private void ReinitializePoolForNewScene()
    {
        if (_textPool != null)
        {
            _textPool.Clear();
        }

        _activeTexts.Clear();

        if (_targetCanvas == null)
        {
            _targetCanvas = FindGameplayCanvas();
        }

        if (_targetCanvas != null)
        {
            InitializePool();
        }
    }

    private FloatingComboText GetFromPool()
    {
        if (_textPool == null)
        {
            Debug.LogError("[FloatingComboTextManager] Pool no inicializado");
            return null;
        }
        return _textPool.Get();
    }

    public void ReturnToPool(FloatingComboText text)
    {
        if (text == null || _textPool == null) return;
        _textPool.Return(text);
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
}

[Serializable]
public class ComboTextData
{
    public string message = "COMBO!";
    public Color color = Color.white;
}