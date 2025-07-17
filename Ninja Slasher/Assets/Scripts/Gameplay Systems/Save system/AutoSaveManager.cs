using UnityEngine;

public class AutoSaveManager : MonoBehaviourSingleton<AutoSaveManager>
{
    private SaveManager saveManager;

    void Start()
    {
        saveManager = SaveManager.Instance;

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
        Debug.Log("[AutoSaveManager] Guardado automatico al cerrar aplicacion");
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            ShowSaveIndicator("Guardando...");
            saveManager?.SaveOnApplicationEvent();
            Debug.Log("[AutoSaveManager] Guardado automatico al pausar aplicacion");
        }
    }

    #endregion

    #region GAMEPLAY_EVENTS
    public void OnLevelCompleted(int levelId, int starsEarned, int enemiesKilled, int maxCombo, float playTime)
    {
        ShowSaveIndicator("Guardando...");

        saveManager.UpdateLevelProgress(levelId + 1); // Desbloquear siguiente nivel
        saveManager.UpdateStars(levelId, starsEarned);
        saveManager.UpdateGameStats(enemiesKilled, maxCombo, playTime, true);

        Debug.Log($"[AutoSaveManager] Nivel {levelId} completado - guardado automatico");
    }

    public void OnLevelFailed(int enemiesKilled, int maxCombo, float playTime)
    {
        ShowSaveIndicator("Guardando...", 1f);

        saveManager.UpdateGameStats(enemiesKilled, maxCombo, playTime, false);

        Debug.Log("[AutoSaveManager] Nivel fallado - guardado automatico");
    }

    public void OnAreaUnlocked(int areaId)
    {
        ShowSaveIndicator("area desbloqueada!");

        saveManager.UnlockArea(areaId);

        Debug.Log($"[AutoSaveManager] area {areaId} desbloqueada - guardado automatico");
    }

    public void OnPowerUpObtained(PowerUpType powerUpType, int quantity = 1)
    {
        ShowSaveIndicator("Guardando...", 1f);

        saveManager.AddPowerUpToInventory(powerUpType, quantity);

        Debug.Log($"[AutoSaveManager] Power up {powerUpType} obtenido - guardado automatico");
    }

    public void OnPowerUpActivated(PowerUpType powerUpType, float duration = 1800f)
    {
        ShowSaveIndicator("Guardando...!");

        saveManager.ActivatePowerUp(powerUpType, duration);

        Debug.Log($"[AutoSaveManager] Power-up {powerUpType} activado - guardado automatico");
    }

    public void OnDailyRewardClaimed(string rewardData)
    {
        ShowSaveIndicator("Guardando...");

        saveManager.SaveDailyRewardData(rewardData);

        Debug.Log("[AutoSaveManager] Recompensa diaria reclamada - guardado automatico");
    }

    public void OnLivesChanged(int newLives, System.DateTime lastRegenTime, bool canRegen)
    {
        ShowSaveIndicator("Guardando...", 1f);

        saveManager.UpdateLives(newLives, lastRegenTime, canRegen);

        Debug.Log($"[AutoSaveManager] Vidas actualizadas a {newLives} - guardado automatico");
    }

    public void OnAudioSettingsChanged(float musicVolume, float sfxVolume)
    {
        ShowSaveIndicator("Guardando...", 1f);

        saveManager.SetMusicVolume(musicVolume);
        saveManager.SetSFXVolume(sfxVolume);

        Debug.Log("[AutoSaveManager] Configuracion de audio cambiada - guardado automatico");
    }

    public void OnPowerUpDeactivated(PowerUpType powerUpType)
    {
        ShowSaveIndicator("Guardando...");

        saveManager.DeactivatePowerUp(powerUpType);

        Debug.Log($"[AutoSaveManager] Power-up {powerUpType} desactivado - guardado automatico");
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
        ShowSaveIndicator("Guardando datos...");

        saveManager?.SaveData();
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