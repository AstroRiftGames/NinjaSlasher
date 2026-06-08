using UnityEngine;

public class GameConfigManager : MonoBehaviourSingleton<GameConfigManager>
{
    [Header("CONFIGURATION")]
    [SerializeField] private GameConfig _config;

    [Header("RUNTIME INFO")]
    [SerializeField] private bool _isInitialized = false;

    public static GameConfig Config
    {
        get
        {
            if (Instance == null)
            {
                return null;
            }

            if (Instance._config == null)
            {
                Debug.LogError("[GameConfigManager] GameConfig no asignado en el Inspector");
                return null;
            }

            return Instance._config;
        }
    }

    public override void Awake()
    {
        base.Awake();
        Initialize();
    }

    private void Initialize()
    {
        if (_config == null)
        {
            return;
        }

        if (!_config.ValidateConfiguration())
        {
            return;
        }

        _isInitialized = true;

        //Debug.Log($"[GameConfigManager] Inicializado correctamente con config: {_config.name}");
    }

    public static bool IsReady()
    {
        return Instance != null && Instance._isInitialized && Instance._config != null;
    }

    public static bool IsTrailerCaptureModeEnabled()
    {
        return IsReady() && Instance._config.trailerCaptureMode;
    }

    public static GameConfig GetConfig()
    {
        if (!IsReady())
        {
            return null;
        }

        return Instance._config;
    }

#if UNITY_EDITOR

    [ContextMenu("Create Default Config")]
    private void CreateDefaultConfig()
    {
        Debug.Log("[GameConfigManager] Para crear un GameConfig:");
        Debug.Log("1. Click derecho en carpeta Assets");
        Debug.Log("2. Create > Game > Game Configuration");
        Debug.Log("3. Arrastra el archivo creado a este componente");
    }
#endif
}
