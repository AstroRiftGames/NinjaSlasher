using UnityEngine;

public class FloatingTextPool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloatingComboText textPrefab;
    [SerializeField] private Transform poolContainer;

    [Header("Configuration")]
    [SerializeField] private int initialPoolSize = 10;

    private GenericPool<FloatingComboText> pool;

    private void Awake()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        if (textPrefab == null)
        {
            Debug.LogError("[FloatingTextPool] Prefab no asignado");
            return;
        }

        if (poolContainer == null)
        {
            poolContainer = transform;
        }

        pool = new GenericPool<FloatingComboText>(
            textPrefab.gameObject,
            initialPoolSize,
            poolContainer,
            "FloatingTextPool"
        );

        pool.OnObjectRetrieved += InitializeText;

        Debug.Log($"[FloatingTextPool] Pool inicializado con {initialPoolSize} textos");
    }

    private void InitializeText(FloatingComboText text)
    {
        if (text != null)
        {
            text.Initialize(this);
        }
    }

    public FloatingComboText Get()
    {
        if (pool == null)
        {
            Debug.LogError("[FloatingTextPool] Pool no inicializado");
            return null;
        }

        return pool.Get();
    }

    public void Return(FloatingComboText text)
    {
        if (text == null || pool == null) return;
        pool.Return(text);
    }

    public void SetPoolSize(int size)
    {
        initialPoolSize = size;

        if (pool != null)
        {
            pool.Clear();
            InitializePool();
        }
    }

    private void OnDestroy()
    {
        if (pool != null)
        {
            pool.Clear();
        }
    }
}