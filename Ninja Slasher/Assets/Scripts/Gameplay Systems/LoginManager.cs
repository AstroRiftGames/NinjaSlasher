using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public sealed class PlayerProfileData
{
    public static readonly PlayerProfileData Guest = new PlayerProfileData("Guest", string.Empty, string.Empty, null, false, false);

    public string DisplayName { get; }
    public string PlayerId { get; }
    public string AvatarUrl { get; }
    public Texture2D AvatarTexture { get; }
    public bool IsSignedIn { get; }
    public bool IsGooglePlayGamesAuthenticated { get; }

    public bool HasAvatar => AvatarTexture != null && AvatarTexture != Texture2D.blackTexture;

    public PlayerProfileData(
        string displayName,
        string playerId,
        string avatarUrl,
        Texture2D avatarTexture,
        bool isSignedIn,
        bool isGooglePlayGamesAuthenticated)
    {
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Guest.DisplayName : displayName;
        PlayerId = playerId ?? string.Empty;
        AvatarUrl = avatarUrl ?? string.Empty;
        AvatarTexture = avatarTexture;
        IsSignedIn = isSignedIn;
        IsGooglePlayGamesAuthenticated = isGooglePlayGamesAuthenticated;
    }
}

public class LoginManager : MonoBehaviourSingleton<LoginManager>
{
    private const string GuestPlayerName = "Guest";
    private const string DefaultAuthenticatedPlayerName = "Player";
    private const float AvatarLoadTimeoutSeconds = 10f;

    [Header("Authentication Settings")]
    [SerializeField] private bool autoSignIn = true;
    [SerializeField] private bool debugMode = true;

    public static event Action<bool> OnAuthenticationStateChanged;
    public static event Action<string> OnSignInCompleted;
    public static event Action<string> OnSignInFailed;
    public static event Action<PlayerProfileData> OnPlayerProfileChanged;

    public bool HasAuthSession =>
        UnityServices.State == ServicesInitializationState.Initialized &&
        AuthenticationService.Instance.IsSignedIn;
    public bool IsSignedIn => HasAuthSession;
    public bool IsGooglePlayGamesSignedIn => IsGooglePlayGamesAuthenticated();
    public string PlayerId => HasAuthSession ? AuthenticationService.Instance.PlayerId : "";
    public string PlayerName { get; private set; } = "";
    public string PlayerAvatarUrl { get; private set; } = "";
    public Texture2D PlayerAvatarTexture { get; private set; }
    public PlayerProfileData CurrentPlayerProfile { get; private set; } = PlayerProfileData.Guest;

    private bool isInitialized = false;
    private bool isInitializing = false;
    private string authToken = "";
    private Coroutine avatarLoadCoroutine;
    private string loadedAvatarUrl = "";
    private bool ownsPlayerAvatarTexture = false;

#if UNITY_EDITOR
    [Header("Debug UI")]
    [SerializeField] private TextMeshProUGUI debugOutputText;
    private System.Text.StringBuilder debugLog = new System.Text.StringBuilder();
#endif

    public override void Awake()
    {
        base.Awake();

        DontDestroyOnLoad(gameObject);

        InitializeServices();
    }

    protected override void OnDestroy()
    {
        StopAvatarLoading();
        base.OnDestroy();
    }

    async void InitializeServices()
    {
        if (isInitialized || isInitializing)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
                Debug.Log("[LoginManager] Already initialized or initializing, skipping...");
#endif
            return;
        }

        isInitializing = true;

        try
        {
            await UnityServicesInitializer.EnsureInitializedAsync();

            PlayGamesPlatform.Activate();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
                Debug.Log("[LoginManager] Unity Gaming Services initialized");
#endif

            isInitialized = true;
            isInitializing = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
                Debug.Log("[LoginManager] Services initialized successfully");
#endif

            if (autoSignIn && !AuthenticationService.Instance.IsSignedIn)
            {
                bool gpgSuccess = await TryAutoSignInWithGooglePlayGames();

                if (gpgSuccess)
                {
                    return;
                }

                bool cachedSuccess = await TrySignInCachedUser();

                if (!cachedSuccess)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (debugMode)
                        Debug.Log("[LoginManager] No cached user, signing in anonymously...");
#endif

                    await SignInAnonymously();
                }
            }
            else if (AuthenticationService.Instance.IsSignedIn)
            {
                RefreshPlayerProfile();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                    Debug.Log($"[LoginManager] User already authenticated: {PlayerId}");
#endif

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
                Debug.Log("[LoginManager] Starting Google Play Games sign-in...");
#endif

            bool gpgSuccess = await AuthenticateWithGooglePlayGames();

            if (!gpgSuccess)
            {
                OnSignInFailed?.Invoke("Google Play Games authentication failed");
                return false;
            }

            RequestFailedException lastNetworkException = null;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    if (attempt > 0) await Task.Delay(2000);
                    await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authToken);
                    lastNetworkException = null;
                    break;
                }
                catch (RequestFailedException ex) when (ex is not AuthenticationException)
                {
                    lastNetworkException = ex;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (debugMode)
                        Debug.LogWarning($"[LoginManager] UGS sign-in intento {attempt + 1}/2 falló: {ex.Message}");
#endif
                }
            }
            if (lastNetworkException != null) throw lastNetworkException;

            RefreshPlayerProfile();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
                Debug.Log($"[LoginManager] Sign-in successful! Player: {PlayerId}");
#endif

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
                Debug.Log("[LoginManager] Starting anonymous sign-in...");
#endif

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            RefreshPlayerProfile(GuestPlayerName);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
                Debug.Log($"[LoginManager] Anonymous sign-in successful! Player: {PlayerId}");
#endif

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
                Debug.Log("[LoginManager] Linking with Google Play Games...");
#endif

            bool gpgSuccess = await AuthenticateWithGooglePlayGames();

            if (!gpgSuccess)
            {
                OnSignInFailed?.Invoke("Google Play Games authentication failed");
                return false;
            }

            await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(authToken);

            RefreshPlayerProfile();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
                Debug.Log("[LoginManager] Account linking successful!");
#endif

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
            authToken = "";
            ResetPlayerProfile();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
                Debug.Log("[LoginManager] User signed out");
#endif

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

            authToken = "";
            ResetPlayerProfile();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
                Debug.Log("[LoginManager] Account deleted successfully");
#endif

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                    Debug.Log("[LoginManager] Attempting to sign in cached user...");
#endif

                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                RefreshPlayerProfile();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                    Debug.Log($"[LoginManager] Cached user signed in: {PlayerId}");
#endif

                OnSignInCompleted?.Invoke(PlayerId);
                OnAuthenticationStateChanged?.Invoke(true);

                return true;
            }
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
                Debug.Log($"[LoginManager] Cached sign-in failed: {ex.Message}");
#endif
        }

        return false;
    }

    private async Task<bool> TryAutoSignInWithGooglePlayGames()
    {
        try
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
                Debug.Log("[LoginManager] Attempting Google Play Games auto sign-in...");
#endif

            bool signInSucceeded = await SignInWithGooglePlayGames();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
                Debug.Log($"[LoginManager] Google Play Games auto sign-in result: {signInSucceeded}");
#endif

            return signInSucceeded;
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
                Debug.Log($"[LoginManager] Google Play Games auto sign-in failed: {ex.Message}");
#endif

            return false;
        }
    }

    private async Task<bool> AuthenticateWithGooglePlayGames()
    {
        var authTcs = new TaskCompletionSource<SignInStatus>();
        PlayGamesPlatform.Instance.Authenticate(status => authTcs.SetResult(status));
        SignInStatus authStatus = await authTcs.Task;

        if (authStatus != SignInStatus.Success)
        {
            Debug.LogError($"[LoginManager] Google Play Games authentication failed: {authStatus}");
            return false;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (debugMode)
            Debug.Log("[LoginManager] Google Play Games authentication successful");
#endif

        for (int attempt = 0; attempt < 2; attempt++)
        {
            if (attempt > 0) await Task.Delay(2000);

            string code = await RequestAuthCodeAsync();

            if (!string.IsNullOrEmpty(code))
            {
                authToken = code;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                    Debug.Log("[LoginManager] Authorization code received");
#endif
                return true;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[LoginManager] Auth code vacío, intento {attempt + 1}/2");
#endif
        }

        Debug.LogError("[LoginManager] No se pudo obtener el auth code tras 2 intentos");
        return false;
    }

    private Task<string> RequestAuthCodeAsync()
    {
        var tcs = new TaskCompletionSource<string>();
        PlayGamesPlatform.Instance.RequestServerSideAccess(true, code => tcs.SetResult(code ?? ""));
        return tcs.Task;
    }

    private void RefreshPlayerProfile(string fallbackName = null)
    {
        PlayerName = ResolvePlayerName(fallbackName);
        RefreshPlayerAvatar();
        PublishPlayerProfile();
    }

    private void ResetPlayerProfile()
    {
        StopAvatarLoading();
        ReleaseOwnedAvatarTexture();

        PlayerName = string.Empty;
        PlayerAvatarUrl = string.Empty;
        PlayerAvatarTexture = null;
        loadedAvatarUrl = string.Empty;

        PublishPlayerProfile();
    }

    private void RefreshPlayerAvatar()
    {
        StopAvatarLoading();

        ReleaseOwnedAvatarTexture();
        PlayerAvatarUrl = string.Empty;
        PlayerAvatarTexture = null;

        if (!IsGooglePlayGamesAuthenticated())
        {
            return;
        }

        PlayGamesLocalUser localUser = PlayGamesPlatform.Instance.localUser as PlayGamesLocalUser;
        PlayerAvatarUrl = NormalizeAvatarUrl(localUser?.AvatarURL ?? PlayGamesPlatform.Instance.GetUserImageUrl());

        if (IsUsableAvatarTexture(localUser?.image))
        {
            AssignPlayerAvatarTexture(localUser.image, false, PlayerAvatarUrl);
            return;
        }

        if (!string.IsNullOrEmpty(PlayerAvatarUrl))
        {
            avatarLoadCoroutine = StartCoroutine(ResolvePlayerAvatar(localUser, PlayerAvatarUrl));
        }
    }

    private System.Collections.IEnumerator ResolvePlayerAvatar(PlayGamesLocalUser localUser, string avatarUrl)
    {
        float timeoutAt = Time.realtimeSinceStartup + AvatarLoadTimeoutSeconds;

        while (Time.realtimeSinceStartup < timeoutAt)
        {
            if (!IsGooglePlayGamesAuthenticated())
            {
                avatarLoadCoroutine = null;
                yield break;
            }

            Texture2D profileTexture = localUser?.image;
            if (IsUsableAvatarTexture(profileTexture))
            {
                avatarLoadCoroutine = null;
                AssignPlayerAvatarTexture(profileTexture, false, avatarUrl);
                yield break;
            }

            yield return null;
        }

        avatarLoadCoroutine = null;

        if (!string.IsNullOrEmpty(avatarUrl))
        {
            avatarLoadCoroutine = StartCoroutine(LoadPlayerAvatarFromUrl(avatarUrl));
        }
    }

    private System.Collections.IEnumerator LoadPlayerAvatarFromUrl(string avatarUrl)
    {
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(avatarUrl);
        request.timeout = Mathf.CeilToInt(AvatarLoadTimeoutSeconds);
        yield return request.SendWebRequest();

        avatarLoadCoroutine = null;

        if (request.result != UnityWebRequest.Result.Success)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.LogWarning($"[LoginManager] Failed to load avatar from '{avatarUrl}': {request.error}");
            }
#endif

            yield break;
        }

        Texture2D avatarTexture = DownloadHandlerTexture.GetContent(request);
        if (!IsUsableAvatarTexture(avatarTexture))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.LogWarning("[LoginManager] Avatar download completed but returned an invalid texture.");
            }
#endif

            yield break;
        }

        AssignPlayerAvatarTexture(avatarTexture, true, avatarUrl);
    }

    private void StopAvatarLoading()
    {
        if (avatarLoadCoroutine == null)
        {
            return;
        }

        StopCoroutine(avatarLoadCoroutine);
        avatarLoadCoroutine = null;
    }

    private void AssignPlayerAvatarTexture(Texture2D avatarTexture, bool ownTexture, string avatarUrl)
    {
        if (!IsUsableAvatarTexture(avatarTexture))
        {
            return;
        }

        if (PlayerAvatarTexture == avatarTexture && ownsPlayerAvatarTexture == ownTexture)
        {
            loadedAvatarUrl = avatarUrl ?? string.Empty;
            return;
        }

        ReleaseOwnedAvatarTexture();

        PlayerAvatarTexture = avatarTexture;
        ownsPlayerAvatarTexture = ownTexture;
        loadedAvatarUrl = avatarUrl ?? string.Empty;
        PublishPlayerProfile();
    }

    private void ReleaseOwnedAvatarTexture()
    {
        if (ownsPlayerAvatarTexture && PlayerAvatarTexture != null)
        {
            Destroy(PlayerAvatarTexture);
        }

        ownsPlayerAvatarTexture = false;
    }

    private void PublishPlayerProfile()
    {
        CurrentPlayerProfile = new PlayerProfileData(
            PlayerName,
            PlayerId,
            PlayerAvatarUrl,
            PlayerAvatarTexture,
            IsSignedIn,
            IsGooglePlayGamesAuthenticated());

        OnPlayerProfileChanged?.Invoke(CurrentPlayerProfile);
    }

    private string ResolvePlayerName(string fallbackName)
    {
        if (IsGooglePlayGamesAuthenticated())
        {
            string displayName = PlayGamesPlatform.Instance.GetUserDisplayName();
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackName))
        {
            return fallbackName;
        }

        if (!string.IsNullOrWhiteSpace(PlayerName))
        {
            return PlayerName;
        }

        return IsSignedIn && !string.IsNullOrEmpty(PlayerId)
            ? DefaultAuthenticatedPlayerName
            : GuestPlayerName;
    }

    private static bool IsUsableAvatarTexture(Texture2D texture)
    {
        return texture != null && texture != Texture2D.blackTexture;
    }

    private static string NormalizeAvatarUrl(string avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            return string.Empty;
        }

        if (avatarUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            return "https://" + avatarUrl.Substring("http://".Length);
        }

        return avatarUrl;
    }

    private bool IsGooglePlayGamesAuthenticated()
    {
        try
        {
            return PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.IsAuthenticated();
        }
        catch
        {
            return false;
        }
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

#if UNITY_EDITOR
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
#endif
}
