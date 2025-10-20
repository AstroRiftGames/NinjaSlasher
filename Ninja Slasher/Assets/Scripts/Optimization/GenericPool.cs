using System;
using System.Collections.Generic;
using UnityEngine;

public class GenericPool<T> where T : MonoBehaviour
{
    private Queue<T> pool = new Queue<T>();
    private GameObject prefab;
    private Transform poolParent;
    private int poolSize;
    private string poolName;

    public Action<T> OnObjectCreated;
    public Action<T> OnObjectRetrieved;
    public Action<T> OnObjectReturned;

    public GenericPool(GameObject prefab, int initialSize, Transform parent = null, string poolName = "GenericPool")
    {
        this.prefab = prefab;
        this.poolSize = initialSize;
        this.poolParent = parent;
        this.poolName = poolName;

        if (prefab.GetComponent<T>() == null)
        {
            return;
        }

        InitializePool();
    }

    void InitializePool()
    {
        if (poolParent == null)
        {
            GameObject parentGO = new GameObject($"{poolName}_Parent");
            poolParent = parentGO.transform;
        }

        for (int i = 0; i < poolSize; i++)
        {
            CreateNewObject();
        }
    }

    T CreateNewObject()
    {
        GameObject newObj = UnityEngine.Object.Instantiate(prefab, poolParent);
        T component = newObj.GetComponent<T>();

        newObj.SetActive(false);
        pool.Enqueue(component);

        OnObjectCreated?.Invoke(component);

        return component;
    }

    public T Get()
    {
        T obj;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            obj = CreateNewObject();
            pool.Dequeue();
        }

        obj.gameObject.SetActive(true);
        OnObjectRetrieved?.Invoke(obj);

        return obj;
    }

    public void Return(T obj)
    {
        if (obj == null)
        {
            return;
        }

        obj.gameObject.SetActive(false);
        obj.transform.SetParent(poolParent);

        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        pool.Enqueue(obj);
        OnObjectReturned?.Invoke(obj);
    }

    public int AvailableCount => pool.Count;

    public int TotalSize => poolSize;

    public int UsedCount => poolSize - pool.Count;

    public void ExpandPool(int additionalSize)
    {
        for (int i = 0; i < additionalSize; i++)
        {
            CreateNewObject();
        }

        poolSize += additionalSize;
    }

    public void Clear()
    {
        while (pool.Count > 0)
        {
            T obj = pool.Dequeue();
            if (obj != null && obj.gameObject != null)
            {
                UnityEngine.Object.Destroy(obj.gameObject);
            }
        }

        poolSize = 0;
    }
}
