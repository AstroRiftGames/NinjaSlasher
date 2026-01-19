using UnityEngine;
using AstroRift.Core.Pooling;

public class FloatingTextPool : MonoBehaviour
{
    [SerializeField] private FloatingComboText textPrefab;
    [SerializeField] private Transform poolContainer;
    [SerializeField] private int initialPoolSize = 10;

    private ObjectPool<FloatingComboText> _pool;

    private void Awake()
    {
        _pool = new ObjectPool<FloatingComboText>(
            textPrefab,
            initialPoolSize,
            poolContainer != null ? poolContainer : transform
        );
    }

    public FloatingComboText Get()
    {
        var text = _pool.Get();

        text.OnRequestDespawn -= HandleTextDespawn;
        text.OnRequestDespawn += HandleTextDespawn;

        return text;
    }

    private void HandleTextDespawn(FloatingComboText text)
    {
        text.OnRequestDespawn -= HandleTextDespawn;
        _pool.Release(text);
    }
}
