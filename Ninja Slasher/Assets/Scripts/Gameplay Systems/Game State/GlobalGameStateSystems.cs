using UnityEngine;

[RequireComponent(typeof(GameStateManager))]
[RequireComponent(typeof(PauseController))]
[RequireComponent(typeof(GameStateBindings))]
public sealed class GlobalGameStateSystems : MonoBehaviour
{
    private static GlobalGameStateSystems _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
