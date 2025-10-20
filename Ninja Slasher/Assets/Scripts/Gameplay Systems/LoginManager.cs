using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class LoginManager : MonoBehaviourSingleton<LoginManager>
{
    [Header("Authentication Settings")]
    [SerializeField] private bool autoSignIn = true;
    [SerializeField] private bool debugMode = true;

    public static event Action<bool> OnAuthenticationStateChanged;
    public static event Action<string> OnSignInCompleted;
    public static event Action<string> OnSignInFailed;

    public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
    public string PlayerId => IsSignedIn ? AuthenticationService.Instance.PlayerId : "";
    public string PlayerName { get; private set; } = "";

    private bool isInitialized = false;
    private bool isInitializing = false;

#if UNITY_ANDROID
    private string authToken = "";
#endif

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
                Debug.Log("[LoginManager] Ya inicializado o inicializando, omitiendo...");
            return;
        }

        isInitializing = true;

        try
        {
            if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
            {
                await UnityServices.InitializeAsync();

                if (debugMode)
                    Debug.Log("[LoginManager] Unity Gaming Services inicializado");
            }
            else
            {
                if (debugMode)
                    Debug.Log("[LoginManager] Unity Gaming Services ya inicializado");
            }

#if UNITY_ANDROID
            PlayGamesPlatform.Activate();
#endif

            isInitialized = true;
            isInitializing = false;

            if (debugMode)
                Debug.Log("[LoginManager] Servicios inicializados correctamente");

            if (autoSignIn && !AuthenticationService.Instance.IsSignedIn)
            {
                bool cachedSuccess = await TrySignInCachedUser();

                if (!cachedSuccess)
                {
                    if (debugMode)
                        Debug.Log("[LoginManager] Sin usuario en caché, iniciando sesión anónima...");

                    await SignInAnonymously();
                }
            }
            else if (AuthenticationService.Instance.IsSignedIn)
            {
                PlayerName = GetPlayerName();
                if (debugMode)
                    Debug.Log($"[LoginManager] Usuario ya autenticado: {PlayerId}");

                OnSignInCompleted?.Invoke(PlayerId);
                OnAuthenticationStateChanged?.Invoke(true);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoginManager] Error al inicializar servicios: {e.Message}");
            isInitializing = false;
        }
    }

    #region Public Methods

#if UNITY_ANDROID
    public async Task<bool> SignInWithGooglePlayGames()
    {
        if (!isInitialized)
        {
            Debug.LogError("[LoginManager] Servicios no inicializados");
            return false;
        }

        try
        {
            if (debugMode)
                Debug.Log("[LoginManager] Iniciando Google Play Games sign-in...");

            bool gpgSuccess = await AuthenticateWithGooglePlayGames();

            if (!gpgSuccess)
            {
                OnSignInFailed?.Invoke("Autenticación de Google Play Games falló");
                return false;
            }

            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authToken);

            PlayerName = GetPlayerName();

            if (debugMode)
                Debug.Log($"[LoginManager] Sign-in exitoso! Jugador: {PlayerId}");

            OnSignInCompleted?.Invoke(PlayerId);
            OnAuthenticationStateChanged?.Invoke(true);

            return true;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"[LoginManager] Autenticación falló: {ex.Message}");
            OnSignInFailed?.Invoke($"Autenticación falló: {ex.Message}");
            return false;
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"[LoginManager] Request falló: {ex.Message}");
            OnSignInFailed?.Invoke($"Request falló: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LoginManager] Error inesperado: {ex.Message}");
            OnSignInFailed?.Invoke($"Error inesperado: {ex.Message}");
            return false;
        }
    }
#endif

    public async Task<bool> SignInAnonymously()
    {
        if (!isInitialized)
        {
            Debug.LogError("[LoginManager] Servicios no inicializados");
            return false;
        }

        try
        {
            if (debugMode)
                Debug.Log("[LoginManager] Iniciando sign-in anónimo...");

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            PlayerName = "Invitado";

            if (debugMode)
                Debug.Log($"[LoginManager] Sign-in anónimo exitoso! Jugador: {PlayerId}");

            OnSignInCompleted?.Invoke(PlayerId);
            OnAuthenticationStateChanged?.Invoke(true);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LoginManager] Sign-in anónimo falló: {ex.Message}");
            OnSignInFailed?.Invoke($"Sign-in anónimo falló: {ex.Message}");
            return false;
        }
    }

#if UNITY_ANDROID
    public async Task<bool> LinkWithGooglePlayGames()
    {
        if (!IsSignedIn)
        {
            Debug.LogError("[LoginManager] No hay usuario iniciado para vincular");
            return false;
        }

        try
        {
            if (debugMode)
                Debug.Log("[LoginManager] Vinculando con Google Play Games...");

            bool gpgSuccess = await AuthenticateWithGooglePlayGames();

            if (!gpgSuccess)
            {
                OnSignInFailed?.Invoke("Autenticación de Google Play Games falló");
                return false;
            }

            await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(authToken);

            PlayerName = GetPlayerName();

            if (debugMode)
                Debug.Log("[LoginManager] Vinculación de cuenta exitosa!");

            OnSignInCompleted?.Invoke(PlayerId);

            return true;
        }
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            Debug.LogError("[LoginManager] Cuenta ya vinculada con otra cuenta");
            OnSignInFailed?.Invoke("Esta cuenta ya está vinculada. Por favor inicia sesión en su lugar.");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LoginManager] Vinculación falló: {ex.Message}");
            OnSignInFailed?.Invoke($"Vinculación falló: {ex.Message}");
            return false;
        }
    }
#endif

    public void SignOut()
    {
        try
        {
            AuthenticationService.Instance.SignOut();
            PlayerName = "";

#if UNITY_ANDROID
            authToken = "";
#endif

            if (debugMode)
                Debug.Log("[LoginManager] Usuario desconectado");

            OnAuthenticationStateChanged?.Invoke(false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LoginManager] Desconexión falló: {ex.Message}");
        }
    }

    public async Task<bool> DeleteAccount()
    {
        try
        {
            await AuthenticationService.Instance.DeleteAccountAsync();

            PlayerName = "";

#if UNITY_ANDROID
            authToken = "";
#endif

            if (debugMode)
                Debug.Log("[LoginManager] Cuenta eliminada exitosamente");

            OnAuthenticationStateChanged?.Invoke(false);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LoginManager] Eliminación de cuenta falló: {ex.Message}");
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
                    Debug.Log("[LoginManager] Intentando iniciar sesión con usuario en caché...");

                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                PlayerName = GetPlayerName();

                if (debugMode)
                    Debug.Log($"[LoginManager] Usuario en caché inició sesión: {PlayerId}");

                OnSignInCompleted?.Invoke(PlayerId);
                OnAuthenticationStateChanged?.Invoke(true);

                return true;
            }
        }
        catch (Exception ex)
        {
            if (debugMode)
                Debug.Log($"[LoginManager] Sign-in en caché falló: {ex.Message}");
        }

        return false;
    }

#if UNITY_ANDROID
    private async Task<bool> AuthenticateWithGooglePlayGames()
    {
        var tcs = new TaskCompletionSource<bool>();

        PlayGamesPlatform.Instance.Authenticate((success) =>
        {
            if (success == SignInStatus.Success)
            {
                if (debugMode)
                    Debug.Log("[LoginManager] Autenticación de Google Play Games exitosa");

                PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
                {
                    if (!string.IsNullOrEmpty(code))
                    {
                        authToken = code;
                        if (debugMode)
                            Debug.Log("[LoginManager] Código de autorización recibido");
                        tcs.SetResult(true);
                    }
                    else
                    {
                        Debug.LogError("[LoginManager] Error al obtener código de autorización");
                        tcs.SetResult(false);
                    }
                });
            }
            else
            {
                Debug.LogError($"[LoginManager] Autenticación de Google Play Games falló: {success}");
                tcs.SetResult(false);
            }
        });

        return await tcs.Task;
    }
#endif

    private string GetPlayerName()
    {
        try
        {
#if UNITY_ANDROID
            if (PlayGamesPlatform.Instance.IsAuthenticated())
            {
                return Social.localUser.userName ?? "Jugador";
            }
#endif
        }
        catch
        {
        }

        return IsSignedIn && !string.IsNullOrEmpty(PlayerId) ? "Jugador" : "Invitado";
    }

    #endregion

    #region Debug Methods

#if UNITY_EDITOR
#if UNITY_ANDROID
    [ContextMenu("Sign In with Google Play Games")]
    public async void DebugSignInGPG()
    {
        await SignInWithGooglePlayGames();
    }
#endif

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

#if UNITY_ANDROID
    [ContextMenu("Device: Full Auth Test")]
    public async void DeviceFullAuthTest()
    {
        LogToScreen("=== DEVICE AUTH TEST START ===");

        LogToScreen($"Package: {Application.identifier}");
        LogToScreen($"GameInfo AppID: {GooglePlayGames.GameInfo.ApplicationId}");
        LogToScreen($"GameInfo WebClient: {GooglePlayGames.GameInfo.WebClientId}");

        LogToScreen("Intentando autenticación...");
        bool result = await SignInWithGooglePlayGames();
        LogToScreen($"Resultado: {(result ? "ÉXITO" : "FALLO")}");

        LogToScreen("=== DEVICE AUTH TEST END ===");
    }
#endif

    #endregion
}