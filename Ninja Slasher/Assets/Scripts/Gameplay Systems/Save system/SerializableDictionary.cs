using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> keys = new();
    [SerializeField] private List<TValue> values = new();

    private Dictionary<TKey, TValue> dict = new();

    public void OnBeforeSerialize()
    {
        keys.Clear(); values.Clear();
        foreach (var kv in dict)
        {
            keys.Add(kv.Key);
            values.Add(kv.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        dict.Clear();
        int count = Math.Min(keys.Count, values.Count);
        for (int i = 0; i < count; i++)
            dict[keys[i]] = values[i];
    }

    public bool TryGetValue(TKey key, out TValue value) => dict.TryGetValue(key, out value);
    public void Add(TKey key, TValue value) => dict[key] = value;
    public bool ContainsKey(TKey key) => dict.ContainsKey(key);
    public int Count => dict.Count;
    public TValue this[TKey key] { get => dict[key]; set => dict[key] = value; }
    public IEnumerable<KeyValuePair<TKey, TValue>> Pairs => dict;
}
