using System.Threading.Tasks;
using UnityEngine;

public class AutoSaveManager : MonoBehaviourSingleton<AutoSaveManager>
{
    private SaveManager saveManager;

    private bool _isSuspended;
    public bool IsSuspended => _isSuspended;

    void Start()
    {
        InitializeSaveManager();

        //if (SaveIndicatorUI.Instance != null)
        //{
        //    Debug.Log("[AutoSaveManager] SaveIndicatorUI encontrado correctamente");
        //}
        //else
        //{
        //    Debug.LogWarning("[AutoSaveManager] SaveIndicatorUI NO encontrado");
        //}

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
        if (!hasFocus && !_isSuspended)
        {
            ShowSaveIndicator("SAVING...");
            saveManager?.SaveOnApplicationEvent();
        }
    }

    void OnApplicationQuit()
    {
        ShowSaveIndicator("SAVING...");
        saveManager?.SaveOnApplicationEvent();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && !_isSuspended)
        {
            ShowSaveIndicator("SAVING...");
            saveManager?.SaveOnApplicationEvent();
        }
    }

    #endregion

    #region GAMEPLAY_EVENTS

    public void OnLevelCompleted(int levelId, int starsEarned, int enemiesKilled, int maxCombo, float playTime)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("SAVING...");
        saveManager.UpdateLevelProgress(levelId + 1);
        saveManager.UpdateStars(levelId, starsEarned);
        saveManager.UpdateGameStats(enemiesKilled, maxCombo, playTime, true);
    }

    public void OnLevelFailed(int enemiesKilled, int maxCombo, float playTime)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("SAVING...", 1f);
        saveManager.UpdateGameStats(enemiesKilled, maxCombo, playTime, false);
    }

    public void OnAreaUnlocked(int areaId)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("SAVING...");
        saveManager.UnlockArea(areaId);
    }

    public void OnPowerUpObtained(PowerUpType powerUpType, int quantity = 1)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("SAVING...", 1f);
        saveManager.AddPowerUpToInventory(powerUpType, quantity);
    }

    public void OnPowerUpActivated(PowerUpType powerUpType, int uses)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("SAVING...");
        saveManager.ActivatePowerUp(powerUpType, uses);
    }

    public void OnPowerUpUsesUpdated(PowerUpType powerUpType, int usesRemaining)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("SAVING...", 0.5f);
        saveManager.UpdatePowerUpUses(powerUpType, usesRemaining);
    }

    public void OnDailyRewardClaimed(string rewardData)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("SAVING...");
        saveManager.SaveDailyRewardData(rewardData);
    }

    public void OnLivesChanged(int newLives, System.DateTime lastRegenTime, bool canRegen)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("SAVING...", 1f);
        saveManager.UpdateLives(newLives, lastRegenTime, canRegen);
    }

    public void OnAudioSettingsChanged(float musicVolume, float sfxVolume)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("SAVING...", 1f);
        saveManager.SetAudioSettings(musicVolume, sfxVolume);
    }

    public void OnPowerUpDeactivated(PowerUpType powerUpType)
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("SAVING...");
        saveManager.DeactivatePowerUp(powerUpType);
    }

    public void OnEmergencyBundleActivated()
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("SAVING...");
        saveManager.RecordEmergencyBundleActivation();
    }

    #endregion

    #region UTILITY_METHODS

    private void ShowSaveIndicator(string message, float duration = 1.5f)
    {
        if (SaveIndicatorUI.Instance != null)
        {
            SaveIndicatorUI.Instance.ShowSaveIndicator(duration, message);
        }
    }

    public void ForceSave()
    {
        if (!CheckSaveManager()) return;

        ShowSaveIndicator("SAVING...");
        saveManager.SaveData();
    }

    [ContextMenu("Test Save Indicator")]
    public void TestSaveIndicator()
    {
        ShowSaveIndicator("Prueba desde AutoSaveManager");
    }

    public void SuspendAutoSave()
    {
        _isSuspended = true;
        Debug.Log("[AutoSaveManager] AutoSave SUSPENDIDO");
    }

    public void ResumeAutoSave()
    {
        _isSuspended = false;
        Debug.Log("[AutoSaveManager] AutoSave REANUDADO");
    }

    public void FactoryResetLocalOnly(bool notify = true)
    {
        if (!CheckSaveManager()) return;

        SuspendAutoSave();
        saveManager.BeginReset();
        try
        {
            ShowSaveIndicator("RESET...");
            saveManager.ResetAllLocalSaves(notify);
            Debug.Log("[AutoSaveManager] FactoryResetLocalOnly completado.");
        }
        finally
        {
            saveManager.EndReset();
            ResumeAutoSave();
        }
    }

    public async Task FactoryResetLocalAndCloudAsync(bool notify = true)
    {
        if (!CheckSaveManager()) return;

        SuspendAutoSave();
        saveManager.BeginReset();
        try
        {
            ShowSaveIndicator("RESET (cloud)...");

            await Task.Yield();
            saveManager.ResetAllLocalSaves(notify);
            Debug.Log("[AutoSaveManager] FactoryResetLocalAndCloudAsync completado.");
        }
        finally
        {
            saveManager.EndReset();
            ResumeAutoSave();
        }
    }

    #endregion
}