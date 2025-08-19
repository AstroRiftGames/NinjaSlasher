using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class AuthManager : MonoBehaviourSingleton<AuthManager>
{
    [Header("Authentication Status")]
    [SerializeField] private bool _isSignedIn = false;
    [SerializeField] private string _playerId = "";

    [Header("Debug")]
    [SerializeField] private bool _enableDebugLogs = true;

    public Action OnSignedIn;
    public Action OnSignedOut;
    public Action<string> OnAuthError;

    private const string SESSION_TOKEN_KEY = "CachedSessionToken";

    public override void Awake()
    {
        base.Awake();
        InitializeUnityServices();
    }

    async void InitializeUnityServices()
    {
        try
        {
            DebugLog("Initializing Unity Services...");

            await UnityServices.InitializeAsync();

            DebugLog("Unity Services initialized");

            SetupAuthenticationEvents();

            await AttemptSessionRestore();
        }
        catch (Exception e)
        {
            DebugLogError($"Failed to initialize Unity Services: {e.Message}");
            OnAuthError?.Invoke(e.Message);
        }
    }

    private async Task AttemptSessionRestore()
    {
        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                DebugLog("Already signed in with existing session");
                return;
            }

            if (AuthenticationService.Instance.SessionTokenExists)
            {
                DebugLog("Found existing session token, attempting to restore...");
                await SignInAnonymously();
                return;
            }

            DebugLog("No existing session found, signing in anonymously...");
            await SignInAnonymously();
        }
        catch (Exception e)
        {
            DebugLogError($"Session restore failed: {e.Message}");
            await SignInAnonymously();
        }
    }

    [ContextMenu("Sign In Anonymously")]
    public async Task SignInAnonymously()
    {
        try
        {
            DebugLog("Attempting anonymous sign in...");

            if (AuthenticationService.Instance.IsSignedIn)
            {
                DebugLog("Already signed in");
                return;
            }

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception e)
        {
            DebugLogError($"Anonymous sign in failed: {e.Message}");
            OnAuthError?.Invoke(e.Message);
        }
    }

    void SetupAuthenticationEvents()
    {
        AuthenticationService.Instance.SignedIn += OnSignedInEvent;
        AuthenticationService.Instance.SignedOut += OnSignedOutEvent;
        AuthenticationService.Instance.SignInFailed += OnSignInFailedEvent;
        AuthenticationService.Instance.Expired += OnSessionExpiredEvent;
    }

    private async void OnSignedInEvent()
    {
        _isSignedIn = true;
        _playerId = AuthenticationService.Instance.PlayerId;

        DebugLog($"Signed in successfully!");
        DebugLog($"Player ID: {_playerId}");
        DebugLog($"Session token exists: {AuthenticationService.Instance.SessionTokenExists}");

        OnSignedIn?.Invoke();

        if (CloudSaveManager.Instance != null)
        {
            await CloudSaveManager.Instance.EnsureInitialSync();
            Debug.Log("[Auth] Initial cloud sync completed successfully");
        }
    }

    private void OnSignedOutEvent()
    {
        _isSignedIn = false;
        _playerId = "";

        DebugLog("Signed out");
        OnSignedOut?.Invoke();
    }

    private void OnSignInFailedEvent(RequestFailedException exception)
    {
        DebugLogError($"Sign in failed: {exception.Message}");
        OnAuthError?.Invoke(exception.Message);
    }

    private void OnSessionExpiredEvent()
    {
        DebugLog("Session expired - attempting to re-authenticate");
        _ = SignInAnonymously();
    }

    public bool IsSignedIn()
    {
        return AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;
    }

    public string GetPlayerId()
    {
        return IsSignedIn() ? AuthenticationService.Instance.PlayerId : "";
    }

    public string GetAccessToken()
    {
        return IsSignedIn() ? AuthenticationService.Instance.AccessToken : "";
    }

    [ContextMenu("Sign Out")]
    public void SignOut()
    {
        try
        {
            DebugLog("Signing out...");
            AuthenticationService.Instance.SignOut();
        }
        catch (Exception e)
        {
            DebugLogError($"Sign out failed: {e.Message}");
        }
    }

    [ContextMenu("Clear Session Token")]
    public void ClearSessionToken()
    {
        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.ClearSessionToken();
                DebugLog("Session token cleared");
            }
            else
            {
                DebugLogError("Cannot clear session token while signed in");
            }
        }
        catch (Exception e)
        {
            DebugLogError($"Failed to clear session token: {e.Message}");
        }
    }

    [ContextMenu("Check Session Info")]
    public void CheckSessionInfo()
    {
        DebugLog($"Is Signed In: {AuthenticationService.Instance.IsSignedIn}");
        DebugLog($"Is Authorized: {AuthenticationService.Instance.IsAuthorized}");
        DebugLog($"Is Expired: {AuthenticationService.Instance.IsExpired}");
        DebugLog($"Session Token Exists: {AuthenticationService.Instance.SessionTokenExists}");
        DebugLog($"Player ID: {AuthenticationService.Instance.PlayerId}");
        DebugLog($"Profile: {AuthenticationService.Instance.Profile}");
    }

    [ContextMenu("Force Trigger Cloud Sync")]
    public async void ForceTriggerCloudSync()
    {
        DebugLog("Manually triggering cloud sync...");
        await CloudSaveManager.Instance.EnsureInitialSync();
    }

    private void DebugLog(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[Auth] {message}");
        }
    }

    private void DebugLogError(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogError($"[Auth] {message}");
        }
    }

    void OnDestroy()
    {
        if (AuthenticationService.Instance != null)
        {
            AuthenticationService.Instance.SignedIn -= OnSignedInEvent;
            AuthenticationService.Instance.SignedOut -= OnSignedOutEvent;
            AuthenticationService.Instance.SignInFailed -= OnSignInFailedEvent;
            AuthenticationService.Instance.Expired -= OnSessionExpiredEvent;
        }
    }
}