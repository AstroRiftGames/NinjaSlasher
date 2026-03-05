using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;

public class LoginManager : MonoBehaviourSingleton<LoginManager>
{
    [Header("Authentication Settings")]
    [SerializeField] private bool autoSignIn = true;
    [SerializeField] private bool debugMode = true;

    public static event Action<bool> OnAuthenticationStateChanged;
    public static event Action<string> OnSignInCompleted;
    public static event Action<string> OnSignInFailed;

    public bool IsSignedIn =>
        UnityServices.State == ServicesInitializationState.Initialized &&
        AuthenticationService.Instance.IsSignedIn;
    public string PlayerId => IsSignedIn ? AuthenticationService.Instance.PlayerId : "";
    public string PlayerName { get; private set; } = "";

    private bool isInitialized = false;
    private bool isInitializing = false;
    private string authToken = "";

    [Header("Debug UI")]
    [SerializeField] private TextMeshProUGUI debugOutputText;
    private System.Text.StringBuilder debugLog = new System.Text.StringBuilder();

    public override void Awake()
    {
        base.Awake();

        DontDestroyOnLoad(gameObject);

        InitializeServices();
    }

    async void InitializeServices()
    {
        if (isInitialized || isInitializing)
        {
            if (debugMode)
                Debug.Log("[LoginManager] Already initialized or initializing, skipping...");
            return;
        }

        isInitializing = true;

        try
        {
            await UnityServicesInitializer.EnsureInitializedAsync();

            PlayGamesPlatform.Activate();

            if (debugMode)
                Debug.Log("[LoginManager] Unity Gaming Services initialized");

            isInitialized = true;
            isInitializing = false;

            if (debugMode)
                Debug.Log("[LoginManager] Services initialized successfully");

            if (autoSignIn && !AuthenticationService.Instance.IsSignedIn)
            {
                bool cachedSuccess = await TrySignInCachedUser();

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

            bool gpgSuccess = await AuthenticateWithGooglePlayGames();

            if (!gpgSuccess)
            {
                OnSignInFailed?.Invoke("Google Play Games authentication failed");
                return false;
            }

            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authToken);

            PlayerName = GetPlayerName();

            if (debugMode)
                Debug.Log($"[LoginManager] Sign-in successful! Player: {PlayerId}");

            OnSignInCompleted?.Invoke(PlayerId);
            OnAuthenticationStateChanged?.Invoke(true);

            return true;
        }
        catch (AuthenticationException ex)
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

            bool gpgSuccess = await AuthenticateWithGooglePlayGames();

            if (!gpgSuccess)
            {
                OnSignInFailed?.Invoke("Google Play Games authentication failed");
                return false;
            }

            await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(authToken);

            PlayerName = GetPlayerName();

            if (debugMode)
                Debug.Log("[LoginManager] Account linking successful!");

            OnSignInCompleted?.Invoke(PlayerId);

            return true;
        }
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
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

    #region Device Debug Methods

    public void LogToScreen(string message)
    {
        string timestampedMessage = $"[{System.DateTime.Now:HH:mm:ss}] {message}";

        Debug.Log($"[DEVICE DEBUG] {timestampedMessage}");

        debugLog.AppendLine(timestampedMessage);

        var lines = debugLog.ToString().Split('\n');
        if (lines.Length > 20)
        {
            debugLog.Clear();
            for (int i = lines.Length - 20; i < lines.Length; i++)
            {
                if (i >= 0 && !string.IsNullOrEmpty(lines[i]))
                    debugLog.AppendLine(lines[i]);
            }
        }

        UpdateDebugText();
    }

    private void UpdateDebugText()
    {
        if (debugOutputText != null)
        {
            debugOutputText.text = debugLog.ToString();

            if (debugOutputText.transform.parent.GetComponent<ScrollRect>() != null)
            {
                var scrollRect = debugOutputText.transform.parent.GetComponent<ScrollRect>();
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }

    [ContextMenu("Clear Debug Text")]
    public void ClearDebugText()
    {
        debugLog.Clear();
        UpdateDebugText();
    }

    [ContextMenu("Device: Full Auth Test")]
    public async void DeviceFullAuthTest()
    {
        LogToScreen("=== DEVICE AUTH TEST START ===");

        LogToScreen($"Package: {Application.identifier}");
        LogToScreen($"GameInfo AppID: {GooglePlayGames.GameInfo.ApplicationId}");
        LogToScreen($"GameInfo WebClient: {GooglePlayGames.GameInfo.WebClientId}");

        if (!isInitialized)
        {
            LogToScreen("Initializing services...");
            await Task.Delay(1000);
        }

        LogToScreen($"Initialized: {isInitialized}");
        LogToScreen($"Unity Services: {UnityServices.State}");

        LogToScreen("Testing Google Play Games...");
        bool gpgResult = await SignInWithGooglePlayGames();
        LogToScreen($"GPG Result: {gpgResult}");

        if (gpgResult)
        {
            LogToScreen($"GPG Success - Player: {PlayerName}");
            LogToScreen($"Unity Auth: {AuthenticationService.Instance.IsSignedIn}");
        }
        else
        {
            LogToScreen("GPG Failed - trying anonymous...");
            bool anonResult = await SignInAnonymously();
            LogToScreen($"Anonymous Result: {anonResult}");
        }

        if (SaveManager.Instance != null)
        {
            LogToScreen("Testing save integration...");
            var gameData = SaveManager.Instance.GetGameData();
            LogToScreen($"Save loaded: {SaveManager.Instance.IsDataLoaded}");
            LogToScreen($"Highest level: {gameData.highestUnlockedLevel}");
        }

        LogToScreen("=== DEVICE AUTH TEST END ===");
    }

    [ContextMenu("Device: GPG Only Test")]
    public async void DeviceGPGOnlyTest()
    {
        LogToScreen("=== GPG ONLY TEST ===");

        try
        {
            LogToScreen($"GPG Platform Active: {PlayGamesPlatform.Instance != null}");
            LogToScreen($"GPG Already Auth: {PlayGamesPlatform.Instance.IsAuthenticated()}");

            if (PlayGamesPlatform.Instance.IsAuthenticated())
            {
                LogToScreen($"Already authenticated: {Social.localUser.userName}");
                return;
            }

            LogToScreen("Starting GPG authentication...");

            var tcs = new TaskCompletionSource<bool>();

            PlayGamesPlatform.Instance.Authenticate((success) =>
            {
                LogToScreen($"GPG Auth Status: {success}");

                if (success == SignInStatus.Success)
                {
                    LogToScreen($"Success! User: {Social.localUser.userName}");
                    LogToScreen($"User ID: {Social.localUser.id}");

                    PlayGamesPlatform.Instance.RequestServerSideAccess(true, (code) =>
                    {
                        if (!string.IsNullOrEmpty(code))
                        {
                            LogToScreen("Auth code received successfully");
                            tcs.SetResult(true);
                        }
                        else
                        {
                            LogToScreen("Failed to get auth code");
                            tcs.SetResult(false);
                        }
                    });
                }
                else
                {
                    LogToScreen($"GPG Auth failed: {success}");
                    tcs.SetResult(false);
                }
            });

            await tcs.Task;
        }
        catch (Exception e)
        {
            LogToScreen($"Exception: {e.Message}");
        }

        LogToScreen("=== GPG TEST END ===");
    }

    [ContextMenu("Device: Check Play Services")]
    public void DeviceCheckPlayServices()
    {
        LogToScreen("=== PLAY SERVICES CHECK ===");

        try
        {
            LogToScreen($"Application.platform: {Application.platform}");
            LogToScreen($"SystemInfo.operatingSystem: {SystemInfo.operatingSystem}");

            LogToScreen($"PlayGamesPlatform exists: {PlayGamesPlatform.Instance != null}");

            if (PlayGamesPlatform.Instance != null)
            {
                LogToScreen($"Platform authenticated: {PlayGamesPlatform.Instance.IsAuthenticated()}");
            }

            LogToScreen($"Unity Services state: {UnityServices.State}");
            LogToScreen($"Auth Service available: {AuthenticationService.Instance != null}");

        }
        catch (Exception e)
        {
            LogToScreen($"Play Services check error: {e.Message}");
        }

        LogToScreen("=== END PLAY SERVICES CHECK ===");
    }

    #endregion
}
