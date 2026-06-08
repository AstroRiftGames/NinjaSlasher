using System;
using UnityEngine;

public class NotificationLifecycleBridge : MonoBehaviourSingleton<NotificationLifecycleBridge>
{
    public static event Action OnAppEnteredBackground;
    public static event Action OnAppReturnedToForeground;

    private bool _isInitialized;
    private bool _isInBackground;

    public override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        _isInitialized = true;
        _isInBackground = false;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!_isInitialized)
            return;

        if (pauseStatus)
            EnterBackground();
        else
            ReturnToForeground();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!_isInitialized || !hasFocus)
            return;

        ReturnToForeground();
    }

    private void EnterBackground()
    {
        if (_isInBackground)
            return;

        _isInBackground = true;
        OnAppEnteredBackground?.Invoke();
    }

    private void ReturnToForeground()
    {
        if (!_isInBackground)
            return;

        _isInBackground = false;
        OnAppReturnedToForeground?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindFirstObjectByType<NotificationLifecycleBridge>() != null)
            return;

        var go = new GameObject("NotificationLifecycleBridge");
        go.AddComponent<NotificationLifecycleBridge>();
    }
}
