using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System.Threading.Tasks;
using System;

public class LoginManager : MonoBehaviourSingleton<LoginManager>
{
    [Header("Authentication Settings")]
    [SerializeField] private bool autoSignIn = true;
    [SerializeField] private bool debugMode = true;

    // Events
    public static event Action<bool> OnAuthenticationStateChanged;
    public static event Action<string> OnSignInCompleted;
    public static event Action<string> OnSignInFailed;

    // Properties
    public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
    public string PlayerId => IsSignedIn ? AuthenticationService.Instance.PlayerId : "";
    public string PlayerName { get; private set; } = "";

    // State
    private bool isInitialized = false;
    private bool isInitializing = false; // Nueva bandera
    private string authToken = "";

    public override void Awake()
    {
        base.Awake();

        // Make persistent across scenes if needed
        DontDestroyOnLoad(gameObject);

        InitializeServices();
    }

    async void InitializeServices()
    {
        // Evitar doble inicialización
        if (isInitialized || isInitializing)
        {
            if (debugMode)
                Debug.Log("[LoginManager] Already initialized or initializing, skipping...");
            return;
        }

        isInitializing = true;

        try
        {
            // Initialize Unity Gaming Services only once
            if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
            {
                await UnityServices.InitializeAsync();

                if (debugMode)
                    Debug.Log("[LoginManager] Unity Gaming Services initialized");
            }
            else
            {
                if (debugMode)
                    Debug.Log("[LoginManager] Unity Gaming Services already initialized");
            }

            // Initialize Google Play Games (safe to call multiple times)
            PlayGamesPlatform.Activate();

            isInitialized = true;
            isInitializing = false;

            if (debugMode)
                Debug.Log("[LoginManager] Services initialized successfully");

            // Auto sign-in if enabled and not already signed in
            if (autoSignIn && !AuthenticationService.Instance.IsSignedIn)
            {
                bool cachedSuccess = await TrySignInCachedUser();

                // If no cached user, sign in anonymously automatically
                if (!cachedSuccess)
                {
                    if (debugMode)
                        Debug.Log("[LoginManager] No cached user, signing in anonymously...");

                    await SignInAnonymously();
                }
            }
            else if (AuthenticationService.Instance.IsSignedIn)
            {
                PlayerName = GetPlayerName();
                if (debugMode)
                    Debug.Log($"[LoginManager] User already authenticated: {PlayerId}");

                OnSignInCompleted?.Invoke(PlayerId);
                OnAuthenticationStateChanged?.Invoke(true);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoginManager] Failed to initialize services: {e.Message}");
            isInitializing = false;
        }
    }

    #region Public Methods

    /// <summary>
    /// Sign in with Google Play Games
    /// </summary>
    public async Task<bool> SignInWithGooglePlayGames()
    {
        if (!isInitialized)
        {
            Debug.LogError("[LoginManager] Services not initialized");
            return false;
        }

        try
        {
            if (debugMode)
                Debug.Log("[LoginManager] Starting Google Play Games sign-in...");

            // First authenticate with Google Play Games
            bool gpgSuccess = await AuthenticateWithGooglePlayGames();

            if (!gpgSuccess)
            {
                OnSignInFailed?.Invoke("Google Play Games authentication failed");
                return false;
            }

            // Then sign in with Unity Authentication
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authToken);

            PlayerName = GetPlayerName();

            if (debugMode)
                Debug.Log($"[LoginManager] Sign-in successful! Player: {PlayerId}");

            OnSignInCompleted?.Invoke(PlayerId);
            OnAuthenticationStateChanged?.Invoke(true);

            return true;
        }
        catch (Unity.Services.Authentication.AuthenticationException ex)
        {
            Debug.LogError($"[LoginManager] Authentication failed: {ex.Message}");
            OnSignInFailed?.Invoke($"Authentication failed: {ex.Message}");
            return false;
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"[LoginManager] Request failed: {ex.Message}");
            OnSignInFailed?.Invoke($"Request failed: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LoginManager] Unexpected error: {ex.Message}");
            OnSignInFailed?.Invoke($"Unexpected error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sign in anonymously
    /// </summary>
    public async Task<bool> SignInAnonymously()
    {
        if (!isInitialized)
        {
            Debug.LogError("[LoginManager] Services not initialized");
            return false;
        }

        try
        {
            if (debugMode)
                Debug.Log("[LoginManager] Starting anonymous sign-in...");

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            PlayerName = "Guest";

            if (debugMode)
                Debug.Log($"[LoginManager] Anonymous sign-in successful! Player: {PlayerId}");

            OnSignInCompleted?.Invoke(PlayerId);
            OnAuthenticationStateChanged?.Invoke(true);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LoginManager] Anonymous sign-in failed: {ex.Message}");
            OnSignInFailed?.Invoke($"Anonymous sign-in failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Link current anonymous account with Google Play Games
    /// </summary>
    public async Task<bool> LinkWithGooglePlayGames()
    {
        if (!IsSignedIn)
        {
            Debug.LogError("[LoginManager] No user signed in to link");
            return false;
        }

        try
        {
            if (debugMode)
                Debug.Log("[LoginManager] Linking with Google Play Games...");

            // First authenticate with Google Play Games
            bool gpgSuccess = await AuthenticateWithGooglePlayGames();

            if (!gpgSuccess)
            {
                OnSignInFailed?.Invoke("Google Play Games authentication failed");
                return false;
            }

            // Link the accounts
            await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(authToken);

            PlayerName = GetPlayerName();

            if (debugMode)
                Debug.Log("[LoginManager] Account linking successful!");

            OnSignInCompleted?.Invoke(PlayerId);

            return true;
        }
        catch (Unity.Services.Authentication.AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            Debug.LogError("[LoginManager] Account already linked with another account");
            OnSignInFailed?.Invoke("This account is already linked. Please sign in instead.");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LoginManager] Linking failed: {ex.Message}");
            OnSignInFailed?.Invoke($"Linking failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sign out current user
    /// </summary>
    public void SignOut()
    {
        try
        {
            AuthenticationService.Instance.SignOut();
            PlayerName = "";
            authToken = "";

            if (debugMode)
                Debug.Log("[LoginManager] User signed out");

            OnAuthenticationStateChanged?.Invoke(false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LoginManager] Sign out failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete current account
    /// </summary>
    public async Task<bool> DeleteAccount()
    {
        try
        {
            await AuthenticationService.Instance.DeleteAccountAsync();

            PlayerName = "";
            authToken = "";

            if (debugMode)
                Debug.Log("[LoginManager] Account deleted successfully");

            OnAuthenticationStateChanged?.Invoke(false);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LoginManager] Account deletion failed: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Private Methods

    private async Task<bool> TrySignInCachedUser()
    {
        try
        {
            if (AuthenticationService.Instance.SessionTokenExists)
            {
                if (debugMode)
                    Debug.Log("[LoginManager] Attempting to sign in cached user...");

                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                PlayerName = GetPlayerName();

                if (debugMode)
                    Debug.Log($"[LoginManager] Cached user signed in: {PlayerId}");

                OnSignInCompleted?.Invoke(PlayerId);
                OnAuthenticationStateChanged?.Invoke(true);

                return true;
            }
        }
        catch (Exception ex)
        {
            if (debugMode)
                Debug.Log($"[LoginManager] Cached sign-in failed: {ex.Message}");
        }

        return false;
    }

    private async Task<bool> AuthenticateWithGooglePlayGames()
    {
        var tcs = new TaskCompletionSource<bool>();

        PlayGamesPlatform.Instance.Authenticate((success) =>
        {
            if (success == SignInStatus.Success)
            {
                if (debugMode)
                    Debug.Log("[LoginManager] Google Play Games authentication successful");

                // Get authorization code
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
                {
                    if (!string.IsNullOrEmpty(code))
                    {
                        authToken = code;
                        if (debugMode)
                            Debug.Log("[LoginManager] Authorization code received");
                        tcs.SetResult(true);
                    }
                    else
                    {
                        Debug.LogError("[LoginManager] Failed to get authorization code");
                        tcs.SetResult(false);
                    }
                });
            }
            else
            {
                Debug.LogError($"[LoginManager] Google Play Games authentication failed: {success}");
                tcs.SetResult(false);
            }
        });

        return await tcs.Task;
    }

    private string GetPlayerName()
    {
        try
        {
            if (PlayGamesPlatform.Instance.IsAuthenticated())
            {
                return Social.localUser.userName ?? "Player";
            }
        }
        catch
        {
            // Fallback if we can't get the name
        }

        return IsSignedIn && !string.IsNullOrEmpty(PlayerId) ? "Player" : "Guest";
    }

    #endregion

    #region Debug Methods

#if UNITY_EDITOR
    [ContextMenu("Sign In with Google Play Games")]
    public async void DebugSignInGPG()
    {
        await SignInWithGooglePlayGames();
    }

    [ContextMenu("Sign In Anonymously")]
    public async void DebugSignInAnonymous()
    {
        await SignInAnonymously();
    }

    [ContextMenu("Sign Out")]
    public void DebugSignOut()
    {
        SignOut();
    }

    [ContextMenu("Print Auth Info")]
    public void DebugPrintAuthInfo()
    {
        Debug.Log($"IsSignedIn: {IsSignedIn}");
        Debug.Log($"PlayerId: {PlayerId}");
        Debug.Log($"PlayerName: {PlayerName}");
    }
#endif

    #endregion
}