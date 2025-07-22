using UnityEngine;

public class AutoSaveManager : MonoBehaviourSingleton<AutoSaveManager>
{
    private SaveManager saveManager;

    void Start()
    {
        InitializeSaveManager();

        if (SaveIndicatorUI.Instance != null)
        {
            Debug.Log("[AutoSaveManager] SaveIndicatorUI encontrado correctamente");
        }
        else
        {
            Debug.LogWarning("[AutoSaveManager] SaveIndicatorUI NO encontrado");
        }

        Application.focusChanged += OnApplicationFocusChanged;
    }

    private void InitializeSaveManager()
    {
        if (SaveManager.Instance != null)
        {
            saveManager = SaveManager.Instance;
            Debug.Log("[AutoSaveManager] SaveManager inicializado correctamente");
        }
        else
        {
            Debug.LogError("[AutoSaveManager] SaveManager.Instance no está disponible en Start");
        }
    }

    private bool CheckSaveManager()
    {
        if (saveManager == null)
        {
            saveManager = SaveManager.Instance;
            if (saveManager == null)
            {
                Debug.LogError("[AutoSaveManager] SaveManager no disponible");
                return false;
            }
        }
        return true;
    }

    void OnDestroy()
    {
        Application.focusChanged -= OnApplicationFocusChanged;
    }

    #region APPLICATION_EVENTS

    void OnApplicationFocusChanged(bool hasFocus)
    {
        if (!hasFocus)
        {
            ShowSaveIndicator("Guardando...");
            saveManager?.SaveOnApplicationEvent();
        }
    }

    void OnApplicationQuit()
    {
        ShowSaveIndicator("Guardando...");
        saveManager?.SaveOnApplicationEvent();
        Debug.Log("[AutoSaveManager] Guardado automático al cerrar aplicación");
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            ShowSaveIndicator("Guardando...");
            saveManager?.SaveOnApplicationEvent();
            Debug.Log("[AutoSaveManager] Guardado automático al pausar aplicación");
        }
    }

    #endregion

    #region GAMEPLAY_EVENTS

    public void OnLevelCompleted(int levelId, int starsEarned, int enemiesKilled, int maxCombo, float playTime)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Guardando...");
        saveManager.UpdateLevelProgress(levelId + 1);
        saveManager.UpdateStars(levelId, starsEarned);
        saveManager.UpdateGameStats(enemiesKilled, maxCombo, playTime, true);
        Debug.Log($"[AutoSaveManager] Nivel {levelId} completado - guardado automático");
    }

    public void OnLevelFailed(int enemiesKilled, int maxCombo, float playTime)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Guardando...", 1f);
        saveManager.UpdateGameStats(enemiesKilled, maxCombo, playTime, false);
        Debug.Log("[AutoSaveManager] Nivel fallado - guardado automático");
    }

    public void OnAreaUnlocked(int areaId)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Área desbloqueada!");
        saveManager.UnlockArea(areaId);
        Debug.Log($"[AutoSaveManager] Área {areaId} desbloqueada - guardado automático");
    }

    public void OnPowerUpObtained(PowerUpType powerUpType, int quantity = 1)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Guardando...", 1f);
        saveManager.AddPowerUpToInventory(powerUpType, quantity);
        Debug.Log($"[AutoSaveManager] Power up {powerUpType} obtenido - guardado automático");
    }

    public void OnPowerUpActivated(PowerUpType powerUpType, float duration = 1800f)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Guardando...");
        saveManager.ActivatePowerUp(powerUpType, duration);
        Debug.Log($"[AutoSaveManager] Power-up {powerUpType} activado - guardado automático");
    }

    public void OnDailyRewardClaimed(string rewardData)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Guardando...");
        saveManager.SaveDailyRewardData(rewardData);
        Debug.Log("[AutoSaveManager] Recompensa diaria reclamada - guardado automático");
    }

    public void OnLivesChanged(int newLives, System.DateTime lastRegenTime, bool canRegen)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Guardando...", 1f);
        saveManager.UpdateLives(newLives, lastRegenTime, canRegen);
        Debug.Log($"[AutoSaveManager] Vidas actualizadas a {newLives} - guardado automático");
    }

    public void OnAudioSettingsChanged(float musicVolume, float sfxVolume)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Guardando...", 1f);
        saveManager.SetMusicVolume(musicVolume);
        saveManager.SetSFXVolume(sfxVolume);
        Debug.Log("[AutoSaveManager] Configuración de audio cambiada - guardado automático");
    }

    public void OnPowerUpDeactivated(PowerUpType powerUpType)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Guardando...");
        saveManager.DeactivatePowerUp(powerUpType);
        Debug.Log($"[AutoSaveManager] Power-up {powerUpType} desactivado - guardado automático");
    }

    #endregion

    #region UTILITY_METHODS

    private void ShowSaveIndicator(string message, float duration = 1.5f)
    {
        if (SaveIndicatorUI.Instance != null)
        {
            SaveIndicatorUI.Instance.ShowSaveIndicator(duration, message);
        }
        else
        {
            Debug.LogWarning("[AutoSaveManager] SaveIndicatorUI no encontrado.");
        }
    }

    public void ForceSave()
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Guardando datos...");
        saveManager.SaveData();
        Debug.Log("[AutoSaveManager] Guardado manual forzado");
    }

    [ContextMenu("Test Save Indicator")]
    public void TestSaveIndicator()
    {
        ShowSaveIndicator("Prueba desde AutoSaveManager");
    }

    void Update()
    {
        if (Time.frameCount % 300 == 0)
        {
            saveManager?.UpdateActivePowerUps();
        }
    }

    #endregion
}