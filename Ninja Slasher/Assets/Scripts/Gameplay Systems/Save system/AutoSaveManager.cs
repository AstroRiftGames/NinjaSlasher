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
        }
    }

    private bool CheckSaveManager()
    {
        if (saveManager == null)
        {
            saveManager = SaveManager.Instance;
            if (saveManager == null)
            {
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
    }

    public void OnLevelFailed(int enemiesKilled, int maxCombo, float playTime)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Guardando...", 1f);
        saveManager.UpdateGameStats(enemiesKilled, maxCombo, playTime, false);
    }

    public void OnAreaUnlocked(int areaId)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Área desbloqueada!");
        saveManager.UnlockArea(areaId);
    }

    public void OnPowerUpObtained(PowerUpType powerUpType, int quantity = 1)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Guardando...", 1f);
        saveManager.AddPowerUpToInventory(powerUpType, quantity);
    }

    public void OnPowerUpActivated(PowerUpType powerUpType, float duration = 1800f)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Guardando...");
        saveManager.ActivatePowerUp(powerUpType, duration);
    }

    public void OnDailyRewardClaimed(string rewardData)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Guardando...");
        saveManager.SaveDailyRewardData(rewardData);
    }

    public void OnLivesChanged(int newLives, System.DateTime lastRegenTime, bool canRegen)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Guardando...", 1f);
        saveManager.UpdateLives(newLives, lastRegenTime, canRegen);
    }

    public void OnAudioSettingsChanged(float musicVolume, float sfxVolume)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Guardando...", 1f);
        saveManager.SetMusicVolume(musicVolume);
        saveManager.SetSFXVolume(sfxVolume);
    }

    public void OnPowerUpDeactivated(PowerUpType powerUpType)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("Guardando...");
        saveManager.DeactivatePowerUp(powerUpType);
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

        CloudSaveManager.Instance?.OnLocalSaveTriggered();
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