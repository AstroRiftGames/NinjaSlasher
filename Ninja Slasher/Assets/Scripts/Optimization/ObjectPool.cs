using System.Collections.Generic;
using UnityEngine;

namespace AstroRift.Core.Pooling
{
    public class ObjectPool<T> where T : Component, IPoolable
    {
        private readonly T prefab;
        private readonly Stack<T> pool = new();
        private readonly Transform parent;

        public ObjectPool(T prefab, int initialSize, Transform parent = null)
        {
            this.prefab = prefab;
            this.parent = parent;

            for (int i = 0; i < initialSize; i++)
                CreateInstance();
        }

        private T CreateInstance()
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(false);
            pool.Push(instance);
            return instance;
        }

        public T Get()
        {
            if (pool.Count == 0)
                CreateInstance();

            T instance = pool.Pop();
            instance.gameObject.SetActive(true);
            instance.OnSpawn();
            return instance;
        }

        public void Release(T instance)
        {
            instance.OnDespawn();
            instance.gameObject.SetActive(false);
            pool.Push(instance);
        }

        public int AvailableCount => pool.Count;
    }
}
