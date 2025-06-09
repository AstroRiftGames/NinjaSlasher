using UnityEngine;

public abstract class MonoBehaviourSingleton<T> : MonoBehaviour where T : Component
{
	private static T _instance;
	public static T Instance
	{
		get
		{
			return _instance;
		}
	}

	public virtual void Awake()
    {
		if (_instance == null)
		{
			_instance = this as T;
		}
		else Destroy(gameObject);
		DontDestroyOnLoad(gameObject);
	}
}
