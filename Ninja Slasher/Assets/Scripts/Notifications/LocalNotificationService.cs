using System;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

public static class LocalNotificationService
{
    private const string LifeFullNotificationIdKey = "Notification_LifeFull_Id";
    private const string DailyRewardNotificationIdKey = "Notification_DailyReward_Id";
    private const string DefaultRetentionChannelId = "retention_channel";
    private const string DefaultRetentionChannelName = "Recordatorios";
    private const string DefaultRetentionChannelDescription = "Recordatorios del juego";

    private static string _retentionChannelId = DefaultRetentionChannelId;
    private static string _retentionChannelName = DefaultRetentionChannelName;
    private static string _retentionChannelDescription = DefaultRetentionChannelDescription;
#if UNITY_ANDROID && !UNITY_EDITOR
    private static bool _channelRegistered;
#endif
    private static bool _debugLogs;

    public static void Initialize(bool debugLogsEnabled)
    {
        _debugLogs = debugLogsEnabled;
    }

    public static void RegisterRetentionChannel(string channelId, string channelName, string channelDescription)
    {
        string safeChannelId = GetSafeValue(channelId, DefaultRetentionChannelId);
        string safeChannelName = GetSafeValue(channelName, DefaultRetentionChannelName);
        string safeChannelDescription = GetSafeValue(channelDescription, DefaultRetentionChannelDescription);

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!string.Equals(_retentionChannelId, safeChannelId, StringComparison.Ordinal))
            _channelRegistered = false;
#endif
        _retentionChannelId = safeChannelId;
        _retentionChannelName = safeChannelName;
        _retentionChannelDescription = safeChannelDescription;

        EnsureRetentionChannelRegistered();
    }

    public static void ScheduleLifeFullNotification(DateTime fireUtc, string title, string body)
    {
        ScheduleRetentionNotification(fireUtc, title, body, LifeFullNotificationIdKey, "Life full notification");
    }

    public static void ScheduleDailyRewardNotification(DateTime fireUtc, string title, string body)
    {
        ScheduleRetentionNotification(fireUtc, title, body, DailyRewardNotificationIdKey, "Daily reward notification");
    }

    public static void CancelLifeFullNotification()
    {
        CancelRetentionNotification(LifeFullNotificationIdKey, "Life full notification");
    }

    public static void CancelDailyRewardNotification()
    {
        CancelRetentionNotification(DailyRewardNotificationIdKey, "Daily reward notification");
    }

    public static void CancelAllRetentionNotifications()
    {
        CancelLifeFullNotification();
        CancelDailyRewardNotification();
    }

    public static bool RequestPermissionIfNeeded()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        PermissionStatus status = AndroidNotificationCenter.UserPermissionToPost;
        if (_debugLogs)
            Debug.Log($"[LocalNotificationService] Notification permission status: {status}");

        if (status == PermissionStatus.Allowed)
            return true;

        if (status == PermissionStatus.NotRequested)
        {
            PermissionRequest request = new PermissionRequest();

            if (_debugLogs)
                Debug.Log($"[LocalNotificationService] Notification permission requested. Request status: {request.Status}");

            return request.Status == PermissionStatus.Allowed;
        }

        if (_debugLogs)
            Debug.Log($"[LocalNotificationService] Notification permission request skipped. Status: {status}");

        return false;
#else
        if (_debugLogs)
            Debug.Log("[LocalNotificationService] [EDITOR] Notification permission request skipped.");

        return true;
#endif
    }

    private static void ScheduleRetentionNotification(
        DateTime fireUtc,
        string title,
        string body,
        string notificationIdKey,
        string notificationName)
    {
        if (fireUtc <= DateTime.UtcNow)
        {
            CancelRetentionNotification(notificationIdKey, notificationName);

            if (_debugLogs)
                Debug.LogWarning($"[LocalNotificationService] {notificationName} called with past fire time. Cancelled.");

            return;
        }

        CancelRetentionNotification(notificationIdKey, notificationName);

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!CanPostNotifications(notificationName))
            return;

        EnsureRetentionChannelRegistered();

        string channelId = GetSafeValue(_retentionChannelId, DefaultRetentionChannelId);

        var notification = new AndroidNotification(title, body, fireUtc)
        {
            ShouldAutoCancel = true
        };

        int notificationId = AndroidNotificationCenter.SendNotification(notification, channelId);
        if (notificationId < 0)
        {
            if (_debugLogs)
                Debug.LogWarning($"[LocalNotificationService] Failed to schedule {notificationName}.");

            return;
        }

        PlayerPrefs.SetInt(notificationIdKey, notificationId);
        PlayerPrefs.Save();

        if (_debugLogs)
            Debug.Log($"[LocalNotificationService] {notificationName} scheduled for {fireUtc:O} (id={notificationId})");
#else
        if (_debugLogs)
            Debug.Log($"[LocalNotificationService] [EDITOR] Would schedule {notificationName} for {fireUtc:O}");
#endif
    }

    private static void CancelRetentionNotification(string notificationIdKey, string notificationName)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (PlayerPrefs.HasKey(notificationIdKey))
        {
            int notificationId = PlayerPrefs.GetInt(notificationIdKey, -1);
            if (notificationId >= 0)
                AndroidNotificationCenter.CancelNotification(notificationId);

            PlayerPrefs.DeleteKey(notificationIdKey);
            PlayerPrefs.Save();

            if (_debugLogs)
                Debug.Log($"[LocalNotificationService] {notificationName} canceled (id={notificationId})");
        }
#else
        if (_debugLogs)
            Debug.Log($"[LocalNotificationService] [EDITOR] Would cancel {notificationName}");
#endif
    }

    private static string GetSafeValue(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static void EnsureRetentionChannelRegistered()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (_channelRegistered)
            return;

        var channel = new AndroidNotificationChannel
        {
            Id = GetSafeValue(_retentionChannelId, DefaultRetentionChannelId),
            Name = GetSafeValue(_retentionChannelName, DefaultRetentionChannelName),
            Description = GetSafeValue(_retentionChannelDescription, DefaultRetentionChannelDescription),
            Importance = Importance.Default
        };

        AndroidNotificationCenter.RegisterNotificationChannel(channel);
        _channelRegistered = true;

        if (_debugLogs)
            Debug.Log($"[LocalNotificationService] Retention channel registered: {channel.Id}");
#endif
    }

    private static bool CanPostNotifications(string notificationName)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        PermissionStatus status = AndroidNotificationCenter.UserPermissionToPost;
        if (status == PermissionStatus.Allowed)
            return true;

        if (_debugLogs)
            Debug.LogWarning($"[LocalNotificationService] {notificationName} permission denied/not granted. Status: {status}");

        return false;
#else
        return true;
#endif
    }
}
