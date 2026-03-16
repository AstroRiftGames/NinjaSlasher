using UnityEngine;

public abstract class MonoBehaviourSingleton<T> : MonoBehaviour where T : Component
{
    private static T _instance;

    public static T Instance => _instance;

    public virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
