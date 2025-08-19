using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using UnityEngine;

public class CloudSaveManager : MonoBehaviourSingleton<CloudSaveManager>
{
    [Header("Cloud Save Settings")]
    [SerializeField] private bool _enableCloudSave = true;
    [SerializeField] private bool _autoSyncOnSignIn = true;
    [SerializeField] private float _syncIntervalMinutes = 5f;

    [Header("Debug")]
    [SerializeField] private bool _enableDebugLogs = true;

    private SaveManager _localSaveManager;
    private AuthManager _authManager;

    private bool _isCloudSaveAvailable = false;
    private bool _isSyncing = false;
    private DateTime _lastSyncTime = DateTime.MinValue;

    public Action OnCloudSaveEnabled;
    public Action OnCloudSaveDisabled;
    public Action OnSyncStarted;
    public Action OnSyncCompleted;
    public Action<string> OnSyncFailed;

    private const string GAME_DATA_KEY = "game_data";
    private const string LAST_SYNC_KEY = "last_sync_timestamp";
    private const string DEVICE_ID_KEY = "device_id";

    public bool IsAvailable => _isCloudSaveAvailable;

    private bool _appliedFromCloud;

    public event Action<GameData> CloudDataApplied;

    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private bool _initialSyncDone;
    private bool _pushQueued;
    private DateTime? _lastSyncUtc;
    private static readonly TimeSpan MinIntervalBetweenSyncs = TimeSpan.FromSeconds(20);

    private TimeSpan TimeSinceLastSync => _lastSyncUtc.HasValue ? DateTime.UtcNow - _lastSyncUtc.Value : TimeSpan.MaxValue;

    public override void Awake()
    {
        base.Awake();
        InitializeComponents();
    }

    void Start()
    {
        if (_localSaveManager == null || _authManager == null)
        {
            DebugLog("Retrying component initialization in Start...");
            InitializeComponents();
        }

        SetupEventListeners();

        if (_enableCloudSave)
        {
            InvokeRepeating(nameof(AutoSync), 60f, _syncIntervalMinutes * 60f);
        }
    }

    public async Task EnsureInitialSync()
    {
        if (_initialSyncDone) return;
        await WaitUntilAvailable();
        await SyncFromCloud();
        _initialSyncDone = true;
    }

    private void InitializeComponents()
    {
        if (_localSaveManager == null)
        {
            _localSaveManager = SaveManager.Instance;
            if (_localSaveManager == null)
            {
                _localSaveManager = FindObjectOfType<SaveManager>();
            }
        }

        if (_authManager == null)
        {
            _authManager = AuthManager.Instance;
            if (_authManager == null)
            {
                _authManager = FindObjectOfType<AuthManager>();
            }
        }

        DebugLog($"SaveManager found: {_localSaveManager != null}");
        DebugLog($"AuthManager found: {_authManager != null}");
    }

    private static DateTime ParseIso(string iso)
    {
        if (string.IsNullOrEmpty(iso)) return DateTime.MinValue;
        if (DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt))
            return dt.ToUniversalTime();
        return DateTime.MinValue;
    }

    private void SetupEventListeners()
    {
        if (_authManager != null)
        {
            _authManager.OnSignedIn += OnUserSignedIn;
            _authManager.OnSignedOut += OnUserSignedOut;
            DebugLog("Event listeners configured successfully");
        }
        else
        {
            DebugLogError("Cannot setup event listeners - AuthManager not found!");
        }
    }

    private async void OnUserSignedIn()
    {
        DebugLog("User signed in - checking cloud save availability");

        _isCloudSaveAvailable = await CheckCloudSaveAvailability();

        if (_isCloudSaveAvailable)
        {
            OnCloudSaveEnabled?.Invoke();

            if (_autoSyncOnSignIn)
            {
                await SyncFromCloud();
            }
        }
    }

    public async Task PerformInitialCloudSync()
    {
        await _syncLock.WaitAsync();
        try
        {
            if (_initialSyncDone) return;
            _isSyncing = true;

            var keys = await CloudSaveService.Instance.Data.Player
                          .LoadAsync(new HashSet<string> { GAME_DATA_KEY, LAST_SYNC_KEY });

            GameData cloudData = null;
            DateTime cloudTime = DateTime.MinValue;

            if (keys.TryGetValue(GAME_DATA_KEY, out var gd))
                cloudData = JsonUtility.FromJson<GameData>(gd.Value.GetAs<string>());

            if (keys.TryGetValue(LAST_SYNC_KEY, out var ts))
                cloudTime = ParseIso(ts.Value.GetAs<string>());

            var local = SaveManager.Instance?.GetGameDataSnapshot() ?? new GameData();
            var localTime = ParseIso(local.lastPlayDateIso);

            bool useCloud = cloudData != null && cloudTime > localTime;
            if (useCloud)
            {
                ApplyCloudDataToLocal(cloudData);
                Debug.Log($"[CloudSave] Pull inicial: aplicado CLOUD (cloud:{cloudTime:o} > local:{localTime:o})");
            }
            else
            {
                Debug.Log($"[CloudSave] Pull inicial: mantenido LOCAL (local:{localTime:o}, cloud:{cloudTime:o})");
            }

            _initialSyncDone = true;
            _lastSyncUtc = DateTime.UtcNow;
        }
        finally
        {
            _isSyncing = false;
            _syncLock.Release();
        }

        if (_pushQueued)
        {
            _pushQueued = false;
            await SyncToCloud();
        }
    }

    private void OnUserSignedOut()
    {
        DebugLog("User signed out - disabling cloud save");
        _isCloudSaveAvailable = false;
        OnCloudSaveDisabled?.Invoke();
    }

    [ContextMenu("Sync To Cloud")]
    private async Task SyncToCloud()
    {
        await _syncLock.WaitAsync();
        try
        {
            _isSyncing = true;

            var data = SaveManager.Instance?.GetGameDataSnapshot() ?? new GameData();
            data.lastPlayDateIso = DateTime.UtcNow.ToString("o");

            var payload = new Dictionary<string, object>
            {
                ["game_data"] = JsonUtility.ToJson(data),
                ["device_id"] = SystemInfo.deviceUniqueIdentifier,
                ["last_sync_timestamp"] = DateTime.UtcNow.ToString("o")
            };

            await Unity.Services.CloudSave.CloudSaveService.Instance.Data.Player.SaveAsync(payload);

            _lastSyncUtc = DateTime.UtcNow;
            Debug.Log("[CloudSave] Subida OK");
        }
        finally
        {
            _isSyncing = false;
            _syncLock.Release();
        }

        if (_pushQueued && TimeSinceLastSync >= MinIntervalBetweenSyncs)
        {
            _pushQueued = false;
            await SyncToCloud();
        }
    }


    [ContextMenu("Sync From Cloud")]
    public async Task SyncFromCloud()
    {
        if (!CanPerformCloudOperation()) return;

        try
        {
            _isSyncing = true;
            OnSyncStarted?.Invoke();

            DebugLog("Syncing game data from cloud...");

            var keys = new HashSet<string> { GAME_DATA_KEY, LAST_SYNC_KEY, DEVICE_ID_KEY };
            var cloudData = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            DebugLog($"Cloud keys found: {string.Join(", ", cloudData.Keys)}");

            if (cloudData.ContainsKey(GAME_DATA_KEY))
            {
                var gameDataJson = cloudData[GAME_DATA_KEY].Value.GetAs<string>();
                var cloudSyncTime = cloudData.ContainsKey(LAST_SYNC_KEY)
                    ? DateTime.Parse(cloudData[LAST_SYNC_KEY].Value.GetAs<string>())
                    : DateTime.MinValue;

                DebugLog($"Cloud data found, sync time: {cloudSyncTime}");
                DebugLog($"Cloud data preview: {gameDataJson.Substring(0, Math.Min(200, gameDataJson.Length))}...");

                if (!CheckSaveManager())
                {
                    DebugLogError("Cannot apply cloud data - SaveManager not available");
                    return;
                }

                var localGameData = _localSaveManager.GetGameData();
                var cloudGameData = JsonUtility.FromJson<GameData>(gameDataJson);

                bool cloudNewerByTime = ShouldUseCloudData(cloudSyncTime, localGameData.lastPlayDate);
                bool localIsEmpty = !HasMeaningfulProgress(localGameData);
                bool cloudHasProgress = HasMeaningfulProgress(cloudGameData);
                bool cloudBeatsLocal = IsCloudProgressBetter(cloudGameData, localGameData);

                DebugLog($"Local data time: {localGameData.lastPlayDate:o}");
                DebugLog($"Cloud newer by time: {cloudNewerByTime}");
                DebugLog($"Local is empty: {localIsEmpty}, Cloud has progress: {cloudHasProgress}, Cloud beats local: {cloudBeatsLocal}");

                if (cloudBeatsLocal || (localIsEmpty && cloudHasProgress) || cloudNewerByTime)
                {
                    DebugLog("Applying CLOUD data to LOCAL (progress/time rule)");
                    ApplyCloudDataToLocal(cloudGameData);
                }
                else
                {
                    DebugLog("Keeping LOCAL and syncing it to CLOUD");
                    await SyncToCloud();
                }
            }
            else
            {
                DebugLog("No cloud data found - uploading local data");
                await SyncToCloud();
            }

            _lastSyncTime = DateTime.Now;
            OnSyncCompleted?.Invoke();
        }
        catch (Exception e)
        {
            DebugLogError($"Failed to sync from cloud: {e.Message}");
            OnSyncFailed?.Invoke(e.Message);
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private bool CheckSaveManager()
    {
        if (_localSaveManager == null)
        {
            _localSaveManager = SaveManager.Instance;
            if (_localSaveManager == null)
            {
                _localSaveManager = FindObjectOfType<SaveManager>();
            }
        }
        return _localSaveManager != null;
    }

    private async Task<bool> CheckCloudSaveAvailability()
    {
        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                DebugLog("Cloud Save not available - user not signed in");
                return false;
            }

            var testData = new Dictionary<string, object> { { "test", "connectivity" } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(testData);

            DebugLog("Cloud Save is available");
            return true;
        }
        catch (Exception e)
        {
            DebugLogError($"Cloud Save not available: {e.Message}");
            return false;
        }
    }

    private bool CanPerformCloudOperation()
    {
        if (!_enableCloudSave)
        {
            DebugLog("Cloud Save is disabled");
            return false;
        }

        if (!_isCloudSaveAvailable)
        {
            DebugLog("Cloud Save not available");
            return false;
        }

        if (_isSyncing)
        {
            DebugLog("Already syncing...");
            return false;
        }

        return true;
    }

    private bool ShouldUseCloudData(DateTime cloudTime, DateTime localTime)
    {
        return cloudTime > localTime.AddMinutes(1);
    }

    private void ApplyCloudDataToLocal(GameData cloudGameData)
    {
        try
        {
            if (cloudGameData == null)
            {
                Debug.LogWarning("[CloudSave] No hay datos de nube para aplicar.");
                return;
            }

            if (_localSaveManager == null)
                _localSaveManager = SaveManager.Instance ?? FindObjectOfType<SaveManager>();

            if (_localSaveManager == null)
            {
                Debug.LogError("[CloudSave] SaveManager no encontrado. No se pueden aplicar datos de nube.");
                return;
            }

            _localSaveManager.ApplyCloudDataToLocal(
                cloudGameData,
                saveImmediately: true,
                triggerCloudEvent: false
            );

            Debug.Log("[CloudSave] Datos de nube aplicados correctamente al guardado local");
            CloudDataApplied?.Invoke(cloudGameData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CloudSave] Error aplicando datos de nube localmente: {ex}");
        }
    }

    private async void AutoSync()
    {
        if (_isCloudSaveAvailable && !_isSyncing)
        {
            DebugLog("Auto-sync triggered");
            await SyncFromCloud();
        }
    }

    public bool IsCloudSaveAvailable => _isCloudSaveAvailable;
    public bool IsSyncing => _isSyncing;
    public DateTime LastSyncTime => _lastSyncTime;

    public async void OnLocalSaveTriggered()
    {
        if (!_isCloudSaveAvailable) { Debug.Log("[CloudSave] No disponible"); return; }

        if (_isSyncing) { _pushQueued = true; return; }

        if (TimeSinceLastSync < MinIntervalBetweenSyncs)
        {
            _pushQueued = true;
            return;
        }

        await SyncToCloud();
    }

    private void DebugLog(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[CloudSave] {message}");
        }
    }

    private void DebugLogError(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogError($"[CloudSave] {message}");
        }
    }

    private static bool HasMeaningfulProgress(GameData d)
    {
        if (d == null) return false;
        int progressedLevels = d.levelProgressData != null ? d.levelProgressData.Count : 0;
        return d.highestUnlockedLevel > 1 || d.totalStars > 0 || progressedLevels > 0;
    }

    private static bool IsCloudProgressBetter(GameData cloud, GameData local)
    {
        if (cloud == null || local == null) return false;

        if (cloud.highestUnlockedLevel != local.highestUnlockedLevel)
            return cloud.highestUnlockedLevel > local.highestUnlockedLevel;

        if (cloud.totalStars != local.totalStars)
            return cloud.totalStars > local.totalStars;

        int cProg = cloud.levelProgressData != null ? cloud.levelProgressData.Count : 0;
        int lProg = local.levelProgressData != null ? local.levelProgressData.Count : 0;
        if (cProg != lProg)
            return cProg > lProg;

        return false;
    }

    public async Task<bool> WaitUntilAvailable(int timeoutMs = 8000, int pollMs = 100)
    {
        int waited = 0;
        while (!_isCloudSaveAvailable && waited < timeoutMs)
        {
            await System.Threading.Tasks.Task.Delay(pollMs);
            waited += pollMs;
        }
        return _isCloudSaveAvailable;
    }

    void OnDestroy()
    {
        if (_authManager != null)
        {
            _authManager.OnSignedIn -= OnUserSignedIn;
            _authManager.OnSignedOut -= OnUserSignedOut;
        }
    }
}