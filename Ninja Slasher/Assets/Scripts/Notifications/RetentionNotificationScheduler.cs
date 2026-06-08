using System;
using UnityEngine;

public class RetentionNotificationScheduler : MonoBehaviourSingleton<RetentionNotificationScheduler>
{
    private GameConfig _config;
    private bool _notificationServiceInitialized;
    private bool _permissionRequestAttempted;
    private bool _isInBackground;

    public override void Awake()
    {
        base.Awake();
    }

    private void OnEnable()
    {
        if (Instance != this)
            return;

        NotificationLifecycleBridge.OnAppEnteredBackground += OnAppEnteredBackground;
        NotificationLifecycleBridge.OnAppReturnedToForeground += OnAppReturnedToForeground;
        GameEvents.OnLivesChanged += OnLivesChanged;
        GameEvents.OnRewardAvailabilityChanged += OnRewardAvailabilityChanged;
        DailyRewardSystem.OnBootstrapped += OnDailyRewardSystemBootstrapped;
    }

    private void OnDisable()
    {
        NotificationLifecycleBridge.OnAppEnteredBackground -= OnAppEnteredBackground;
        NotificationLifecycleBridge.OnAppReturnedToForeground -= OnAppReturnedToForeground;
        GameEvents.OnLivesChanged -= OnLivesChanged;
        GameEvents.OnRewardAvailabilityChanged -= OnRewardAvailabilityChanged;
        DailyRewardSystem.OnBootstrapped -= OnDailyRewardSystemBootstrapped;
    }

    private void Start()
    {
        RefreshConfig();
        ConfigureNotificationService(requestPermission: true);
    }

    private void OnAppEnteredBackground()
    {
        _isInBackground = true;

        if (CanScheduleLifeFullNotification())
            ScheduleLifeFullNotification();

        if (CanScheduleDailyRewardNotification())
            ScheduleDailyRewardNotification();
    }

    private void OnAppReturnedToForeground()
    {
        _isInBackground = false;
        LocalNotificationService.CancelLifeFullNotification();
        LocalNotificationService.CancelDailyRewardNotification();
    }

    private void OnLivesChanged(int newLives)
    {
        RefreshConfig();

        if (_config == null || !_config.enableLifeFullNotification || _config.infiniteLives || _config.trailerCaptureMode)
        {
            LocalNotificationService.CancelLifeFullNotification();
            return;
        }

        LifeManager lifeManager = LifeManager.Instance;
        if (lifeManager == null || !lifeManager.IsInitialized)
            return;

        if (lifeManager.CurrentLives >= lifeManager.GetMaxLives() || lifeManager.HasTimedUnlimitedLives)
            LocalNotificationService.CancelLifeFullNotification();
    }

    private void OnRewardAvailabilityChanged(bool isAvailable)
    {
        RefreshConfig();

        if (_config == null || !_config.enableDailyRewardNotification)
        {
            LogDebug("[RetentionNotificationScheduler] Daily notification disabled.");
            LocalNotificationService.CancelDailyRewardNotification();
            return;
        }

        if (isAvailable)
        {
            LogDebug("[RetentionNotificationScheduler] Daily reward already available, canceling notification.");
            LocalNotificationService.CancelDailyRewardNotification();
            return;
        }

        if (!_isInBackground)
        {
            LocalNotificationService.CancelDailyRewardNotification();
            return;
        }

        if (CanScheduleDailyRewardNotification())
            ScheduleDailyRewardNotification();
    }

    private void OnDailyRewardSystemBootstrapped()
    {
        if (!_isInBackground)
            return;

        if (CanScheduleDailyRewardNotification())
            ScheduleDailyRewardNotification();
    }

    private void RefreshConfig()
    {
        if (_config != null)
            return;

        if (GameConfigManager.IsReady())
        {
            _config = GameConfigManager.Config;
            ConfigureNotificationService(requestPermission: false);
        }
    }

    private void ConfigureNotificationService(bool requestPermission)
    {
        if (_config == null)
            return;

        if (!_notificationServiceInitialized)
        {
            LocalNotificationService.Initialize(_config.notificationDebugLogs);
            RegisterRetentionChannel();
            _notificationServiceInitialized = true;
        }

        if (requestPermission &&
            !_permissionRequestAttempted &&
            (_config.enableLifeFullNotification || _config.enableDailyRewardNotification))
        {
            _permissionRequestAttempted = true;
            LocalNotificationService.RequestPermissionIfNeeded();
        }
    }

    private void RegisterRetentionChannel()
    {
        if (_config == null)
            return;

        LocalNotificationService.RegisterRetentionChannel(
            _config.notificationAndroidChannelId,
            _config.notificationAndroidChannelName,
            _config.notificationAndroidChannelDescription);
    }

    private bool CanScheduleLifeFullNotification()
    {
        RefreshConfig();

        if (_config == null || !_config.enableLifeFullNotification || _config.infiniteLives || _config.trailerCaptureMode)
        {
            LocalNotificationService.CancelLifeFullNotification();
            return false;
        }

        LifeManager lifeManager = LifeManager.Instance;
        if (lifeManager == null || !lifeManager.IsInitialized)
            return false;

        if (lifeManager.CurrentLives >= lifeManager.GetMaxLives())
        {
            LocalNotificationService.CancelLifeFullNotification();
            return false;
        }

        if (lifeManager.HasTimedUnlimitedLives)
        {
            LocalNotificationService.CancelLifeFullNotification();
            return false;
        }

        return true;
    }

    private void ScheduleLifeFullNotification()
    {
        RefreshConfig();
        if (_config == null)
            return;

        RegisterRetentionChannel();

        LifeManager lifeManager = LifeManager.Instance;
        if (lifeManager == null)
            return;

        if (!lifeManager.TryGetFullLivesUtc(out DateTime fullLivesUtc))
            return;

        LocalNotificationService.ScheduleLifeFullNotification(
            fullLivesUtc,
            _config.lifeFullNotificationTitle,
            _config.lifeFullNotificationBody);
    }

    private bool CanScheduleDailyRewardNotification()
    {
        RefreshConfig();

        if (_config == null || !_config.enableDailyRewardNotification)
        {
            LogDebug("[RetentionNotificationScheduler] Daily notification disabled.");
            LocalNotificationService.CancelDailyRewardNotification();
            return false;
        }

        DailyRewardSystem dailyRewardSystem = DailyRewardSystem.Instance;
        if (dailyRewardSystem == null)
        {
            LogDebug("[RetentionNotificationScheduler] Daily reward system missing.");
            return false;
        }

        if (!dailyRewardSystem.IsBootstrapped)
        {
            LogDebug("[RetentionNotificationScheduler] Daily reward system not bootstrapped.");
            return false;
        }

        if (dailyRewardSystem.CanClaimToday())
        {
            LogDebug("[RetentionNotificationScheduler] Daily reward already available, canceling notification.");
            LocalNotificationService.CancelDailyRewardNotification();
            return false;
        }

        return true;
    }

    private void ScheduleDailyRewardNotification()
    {
        RefreshConfig();
        if (_config == null)
            return;

        RegisterRetentionChannel();

        DailyRewardSystem dailyRewardSystem = DailyRewardSystem.Instance;
        if (dailyRewardSystem == null)
        {
            LogDebug("[RetentionNotificationScheduler] Daily reward system missing.");
            return;
        }

        if (!dailyRewardSystem.IsBootstrapped)
        {
            LogDebug("[RetentionNotificationScheduler] Daily reward system not bootstrapped.");
            return;
        }

        DateTime fireUtc = GetDailyRewardFireUtc(dailyRewardSystem);
        if (fireUtc <= DateTime.UtcNow)
        {
            LogDebug("[RetentionNotificationScheduler] Daily reward fire time is invalid or in the past.");
            return;
        }

        LogDebug($"[RetentionNotificationScheduler] Scheduling daily reward notification for UTC {fireUtc:O}");

        LocalNotificationService.ScheduleDailyRewardNotification(
            fireUtc,
            _config.dailyRewardNotificationTitle,
            _config.dailyRewardNotificationBody);
    }

    private DateTime GetDailyRewardFireUtc(DailyRewardSystem dailyRewardSystem)
    {
        if (_config.useDebugDailyRewardNotificationDelay)
        {
            int delaySeconds = Mathf.Max(1, _config.debugDailyRewardNotificationDelaySeconds);
            return DateTime.UtcNow.AddSeconds(delaySeconds);
        }

        return dailyRewardSystem.GetNextRewardAvailabilityUtc();
    }

    private void LogDebug(string message)
    {
        if (_config != null && _config.notificationDebugLogs)
            Debug.Log(message);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindFirstObjectByType<RetentionNotificationScheduler>() != null)
            return;

        var go = new GameObject("RetentionNotificationScheduler");
        go.AddComponent<RetentionNotificationScheduler>();
    }
}
