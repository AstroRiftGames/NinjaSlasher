using System.Collections.Generic;
using UnityEngine;

public class FloatingTextPool : MonoBehaviour
{
    [SerializeField] private FloatingComboText textPrefab;
    [SerializeField] private Transform poolContainer;
    [SerializeField] private int initialPoolSize = 10;

    private readonly List<FloatingComboText> _activeTexts = new();
    private ObjectPool<FloatingComboText> _pool;

    private Transform PoolRoot => poolContainer != null ? poolContainer : transform;

    private void Awake()
    {
        _pool = new ObjectPool<FloatingComboText>(
            textPrefab,
            initialPoolSize,
            PoolRoot
        );
    }

    private void OnDisable()
    {
        ReleaseAllActive();
    }

    public FloatingComboText Get()
    {
        FloatingComboText text = _pool.Get();

        text.OnRequestDespawn -= HandleTextDespawn;
        text.OnRequestDespawn += HandleTextDespawn;

        _activeTexts.Add(text);
        return text;
    }

    public void ReleaseAllActive()
    {
        for (int i = _activeTexts.Count - 1; i >= 0; i--)
        {
            ReleaseActiveText(_activeTexts[i]);
        }
    }

    private void HandleTextDespawn(FloatingComboText text)
    {
        ReleaseActiveText(text);
    }

    private void ReleaseActiveText(FloatingComboText text)
    {
        if (text == null)
            return;

        text.OnRequestDespawn -= HandleTextDespawn;

        if (!_activeTexts.Remove(text))
            return;

        _pool.Release(text);
    }
}
