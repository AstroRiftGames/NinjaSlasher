using System;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Configuración")]
    public bool enableEncryption = true;
    public bool autoSave = true;
    public float autoSaveInterval = 30f; //SEGUNDOS

    private GameData gameData;
    private string saveFilePath;
    private string backupFilePath;
    private float autoSaveTimer;

    public event Action<GameData> OnDataLoaded;
    public event Action OnDataSaved;
    public event Action<int> OnLivesChanged;
    public event Action<int> OnStarsChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSaveSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        LoadGame();
    }

    void Update()
    {
        if (autoSave)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                SaveGame();
                autoSaveTimer = 0f;
            }
        }

        if (gameData != null)
        {
            gameData.totalPlayTime += Time.deltaTime;
        }

        ProcessLifeRegeneration();

        // POWER UPS VENCIDOS
        CleanExpiredPowerUps();
    }

    private void InitializeSaveSystem()
    {
        string savePath = Application.persistentDataPath;
        saveFilePath = Path.Combine(savePath, "ninja_save.dat");
        backupFilePath = Path.Combine(savePath, "ninja_save_backup.dat");

        Debug.Log($"Sistema de guardado inicializado. Ruta: {saveFilePath}");
    }

    public void SaveGame()
    {
        try
        {
            if (gameData == null)
            {
                Debug.LogWarning("No hay datos para guardar");
                return;
            }

            gameData.lastPlayDate = DateTime.Now;

            string jsonData = JsonUtility.ToJson(gameData, true);

            if (enableEncryption)
            {
                jsonData = EncryptData(jsonData);
            }

            // BACKUP
            if (File.Exists(saveFilePath))
            {
                File.Copy(saveFilePath, backupFilePath, true);
            }

            File.WriteAllText(saveFilePath, jsonData);

            OnDataSaved?.Invoke();
            Debug.Log("Datos guardados exitosamente");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al guardar: {e.Message}");
        }
    }

    public void LoadGame()
    {
        try
        {
            string jsonData = "";

            // CARGA ARCHIVO PRINCIPAL
            if (File.Exists(saveFilePath))
            {
                jsonData = File.ReadAllText(saveFilePath);
            }
            // INTENTAR CON BACKUP
            else if (File.Exists(backupFilePath))
            {
                Debug.LogWarning("Archivo principal corrupto, cargando backup");
                jsonData = File.ReadAllText(backupFilePath);
            }
            else
            {
                Debug.Log("No se encontró archivo de guardado, creando nuevo");
                CreateNewGameData();
                return;
            }

            if (enableEncryption)
            {
                jsonData = DecryptData(jsonData);
            }

            gameData = JsonUtility.FromJson<GameData>(jsonData);

            if (gameData == null)
            {
                throw new Exception("Datos corruptos");
            }

            OnDataLoaded?.Invoke(gameData);
            Debug.Log("Datos cargados exitosamente");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al cargar: {e.Message}. Creando nuevo archivo");
            CreateNewGameData();
        }
    }

    private void CreateNewGameData()
    {
        gameData = new GameData();
        OnDataLoaded?.Invoke(gameData);
        SaveGame();
    }

    #region PUBLIC METHODS
    public void CompleteLevel(int levelId, int starsEarned)
    {
        if (gameData == null) return;

        // Actualizar estrellas del nivel
        if (!gameData.levelStars.ContainsKey(levelId) ||
            gameData.levelStars[levelId] < starsEarned)
        {
            int previousStars = gameData.levelStars.ContainsKey(levelId) ?
                               gameData.levelStars[levelId] : 0;

            gameData.levelStars[levelId] = starsEarned;
            gameData.totalStars += (starsEarned - previousStars);

            OnStarsChanged?.Invoke(gameData.totalStars);
        }

        // Desbloquear siguiente nivel
        if (levelId >= gameData.highestUnlockedLevel)
        {
            gameData.highestUnlockedLevel = levelId + 1;

            int newArea = Mathf.FloorToInt((levelId - 1) / 10) + 1;
            if (newArea > gameData.currentArea)
            {
                gameData.currentArea = newArea;
                Debug.Log($"¡Nueva área desbloqueada! Área {newArea}");
            }
        }

        gameData.totalGamesPlayed++;
        SaveGame();
    }

    public void UseLife()
    {
        if (gameData == null || gameData.currentLives <= 0) return;

        gameData.currentLives--;
        OnLivesChanged?.Invoke(gameData.currentLives);

        if (gameData.currentLives == 4) // Empezar regeneración cuando baje del maximo
        {
            gameData.lastLifeRegenTime = DateTime.Now;
            gameData.canRegenLives = true;
        }

        SaveGame();
    }

    public void AddLife()
    {
        if (gameData == null || gameData.currentLives >= 5) return;

        gameData.currentLives++;
        OnLivesChanged?.Invoke(gameData.currentLives);

        if (gameData.currentLives >= 5)
        {
            gameData.canRegenLives = false;
        }

        SaveGame();
    }

    public void ActivatePowerUp(PowerUpType type, float duration)
    {
        if (gameData == null) return;

        // Remover power-up existente del mismo tipo
        gameData.activePowerUps.RemoveAll(p => p.type == type);

        // Agregar nuevo power-up
        PowerUpData newPowerUp = new PowerUpData
        {
            type = type,
            activationTime = DateTime.Now,
            duration = duration
        };

        gameData.activePowerUps.Add(newPowerUp);
        Debug.Log($"Power-up {type} activado por {duration} segundos");

        SaveGame();
    }

    public bool HasActivePowerUp(PowerUpType type)
    {
        if (gameData == null) return false;

        return gameData.activePowerUps.Exists(p => p.type == type && !p.IsExpired());
    }

    public float GetPowerUpRemainingTime(PowerUpType type)
    {
        if (gameData == null) return 0f;

        PowerUpData powerUp = gameData.activePowerUps.Find(p => p.type == type && !p.IsExpired());
        return powerUp?.GetRemainingTime() ?? 0f;
    }
    #endregion

    private void ProcessLifeRegeneration()
    {
        if (gameData == null || !gameData.canRegenLives || gameData.currentLives >= 5) return;

        // 30 minutos = 1800 segundos
        double timeSinceLastRegen = (DateTime.Now - gameData.lastLifeRegenTime).TotalSeconds;

        if (timeSinceLastRegen >= 1800) // 30 minutos
        {
            int livesToAdd = Mathf.FloorToInt((float)timeSinceLastRegen / 1800f);
            livesToAdd = Mathf.Min(livesToAdd, 5 - gameData.currentLives);

            if (livesToAdd > 0)
            {
                gameData.currentLives += livesToAdd;
                gameData.lastLifeRegenTime = gameData.lastLifeRegenTime.AddSeconds(livesToAdd * 1800);

                OnLivesChanged?.Invoke(gameData.currentLives);

                if (gameData.currentLives >= 5)
                {
                    gameData.canRegenLives = false;
                }

                Debug.Log($"Regeneradas {livesToAdd} vidas. Total: {gameData.currentLives}");
            }
        }
    }

    private void CleanExpiredPowerUps()
    {
        if (gameData == null) return;

        int initialCount = gameData.activePowerUps.Count;
        gameData.activePowerUps.RemoveAll(p => p.IsExpired());

        if (gameData.activePowerUps.Count < initialCount)
        {
            Debug.Log("Power-ups expirados removidos");
        }
    }

    #region GETTERS

    public GameData GetGameData() => gameData;
    public int GetCurrentLives() => gameData?.currentLives ?? 5;
    public int GetTotalStars() => gameData?.totalStars ?? 0;
    public int GetHighestUnlockedLevel() => gameData?.highestUnlockedLevel ?? 1;
    public int GetCurrentArea() => gameData?.currentArea ?? 1;
    public int GetLevelStars(int levelId) => gameData?.levelStars.ContainsKey(levelId) == true ? gameData.levelStars[levelId] : 0;

    public TimeSpan GetTimeUntilNextLife()
    {
        if (gameData == null || gameData.currentLives >= 5) return TimeSpan.Zero;

        DateTime nextLifeTime = gameData.lastLifeRegenTime.AddMinutes(30);
        TimeSpan remaining = nextLifeTime - DateTime.Now;

        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    #endregion

    #region SETTINGS

    public void SetMusicVolume(float volume)
    {
        if (gameData == null) return;
        gameData.musicVolume = Mathf.Clamp01(volume);
        SaveGame();
    }

    public void SetSFXVolume(float volume)
    {
        if (gameData == null) return;
        gameData.sfxVolume = Mathf.Clamp01(volume);
        SaveGame();
    }

    public float GetMusicVolume() => gameData?.musicVolume ?? 1f;
    public float GetSFXVolume() => gameData?.sfxVolume ?? 1f;

    #endregion

    #region UTILITIES
    
    public void DeleteSaveData()
    {
        try
        {
            if (File.Exists(saveFilePath))
                File.Delete(saveFilePath);
            if (File.Exists(backupFilePath))
                File.Delete(backupFilePath);

            CreateNewGameData();
            Debug.Log("Datos de guardado eliminados");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al eliminar datos: {e.Message}");
        }
    }
    #endregion

    private string EncryptData(string data)
    {
        // Encriptacion simple XOR
        char[] chars = data.ToCharArray();
        string key = "NinjaSlasher2025";

        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = (char)(chars[i] ^ key[i % key.Length]);
        }

        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(new string(chars)));
    }

    private string DecryptData(string encryptedData)
    {
        byte[] data = Convert.FromBase64String(encryptedData);
        string decryptedString = System.Text.Encoding.UTF8.GetString(data);

        char[] chars = decryptedString.ToCharArray();
        string key = "NinjaSlasher2025";

        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = (char)(chars[i] ^ key[i % key.Length]);
        }

        return new string(chars);
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGame();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveGame();
        }
    }

    void OnDestroy()
    {
        SaveGame();
    }
}
