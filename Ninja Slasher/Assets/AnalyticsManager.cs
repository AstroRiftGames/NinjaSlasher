using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;
using Unity.Services.Analytics.Data;
using System.Threading.Tasks;
using System;

public class AnalyticsManager : MonoBehaviourSingleton<AnalyticsManager>
{
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool enableAnalyticsInEditor = true;

    [SerializeField] private bool isInitialized = false;
    [SerializeField] private bool isDataCollectionActive = false;

    public override void Awake()
    {
        base.Awake();
    }

    private async void Start()
    {
        await InitializeAnalytics();
    }

    private async Task InitializeAnalytics()
    {
        try
        {
            await UnityServices.InitializeAsync();

            Debug.Log("Unity Services inicializado correctamente");

            if (AnalyticsService.Instance != null)
            {
                AnalyticsService.Instance.StartDataCollection();

                isInitialized = true;
                isDataCollectionActive = true;

                Debug.Log("Unity Analytics inicializado y recolección de datos iniciada");

                RecordGameStart();
            }
            else
            {
                Debug.LogError("[AnalyticsManager] AnalyticsService no está disponible");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error al inicializar Analytics: {e.Message}");
        }
    }

    public void RecordGameStart()
    {
        if (!CanRecordEvent()) return;

        try
        {
            var gameStartEvent = new CustomEvent("gameStart")
            {
                { "sessionId", Guid.NewGuid().ToString() },
                { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") },
                { "platform", Application.platform.ToString() },
                { "version", Application.version }
            };

            AnalyticsService.Instance.RecordEvent(gameStartEvent);
            Debug.Log("Evento 'gameStart' enviado correctamente");
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando gameStart: {e.Message}");
        }
    }

    public void RecordGameEnd(float sessionDuration)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var gameEndEvent = new CustomEvent("gameEnd")
            {
                { "sessionDuration", sessionDuration },
                { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") },
                { "platform", Application.platform.ToString() }
            };

            AnalyticsService.Instance.RecordEvent(gameEndEvent);
            Debug.Log($"Evento 'gameEnd' enviado - Duración: {sessionDuration} segundos");
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando gameEnd: {e.Message}");
        }
    }

    public void RecordLevelCompleted(int levelId, int starsEarned, float completionTime)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var levelCompletedEvent = new CustomEvent("levelCompleted")
            {
                { "levelId", levelId },
                { "starsEarned", starsEarned },
                { "completionTime", completionTime },
                { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
            };

            AnalyticsService.Instance.RecordEvent(levelCompletedEvent);
            Debug.Log($"Evento 'levelCompleted' enviado - Nivel: {levelId}, Estrellas: {starsEarned}, Tiempo: {completionTime}s");
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando levelCompleted: {e.Message}");
        }
    }

    public void RecordLevelFailed(int levelId, string failReason, float attemptTime, int attemptNumber = 1)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var levelFailedEvent = new CustomEvent("levelFailed")
            {
                { "levelId", levelId },
                { "failReason", failReason },
                { "attemptTime", attemptTime },
                { "attemptNumber", attemptNumber },
                { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
            };

            AnalyticsService.Instance.RecordEvent(levelFailedEvent);
            Debug.Log($"Evento 'levelFailed' enviado - Nivel: {levelId}, Razón: {failReason}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando levelFailed: {e.Message}");
        }
    }

    public void RecordLifeLost(int currentLives, int totalLivesLost, string lossReason)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var lifeLostEvent = new CustomEvent("lifeLost")
            {
                { "currentLives", currentLives },
                { "totalLivesLost", totalLivesLost },
                { "lossReason", lossReason },
                { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
            };

            AnalyticsService.Instance.RecordEvent(lifeLostEvent);
            Debug.Log($"Evento 'lifeLost' enviado - Vidas restantes: {currentLives}, Razón: {lossReason}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando lifeLost: {e.Message}");
        }
    }

    public void RecordLifeRestored(int newLifeCount, string restoreMethod)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var lifeRestoredEvent = new CustomEvent("lifeRestored")
            {
                { "newLifeCount", newLifeCount },
                { "restoreMethod", restoreMethod },
                { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
            };

            AnalyticsService.Instance.RecordEvent(lifeRestoredEvent);
            Debug.Log($"Evento 'lifeRestored' enviado - Nuevas vidas: {newLifeCount}, Método: {restoreMethod}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando lifeRestored: {e.Message}");
        }
    }

    public void RecordPlayerAction(string actionType, string actionDetails = "", int currentLevel = -1)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var playerActionEvent = new CustomEvent("playerAction")
            {
                { "actionType", actionType },
                { "actionDetails", actionDetails },
                { "currentLevel", currentLevel },
                { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
            };

            AnalyticsService.Instance.RecordEvent(playerActionEvent);
            Debug.Log($"Evento 'playerAction' enviado - Acción: {actionType}, Detalles: {actionDetails}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando playerAction: {e.Message}");
        }
    }

    public void RecordScreenTransition(string fromScreen, string toScreen)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var screenTransitionEvent = new CustomEvent("screenTransition")
            {
                { "fromScreen", fromScreen },
                { "toScreen", toScreen },
                { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
            };

            AnalyticsService.Instance.RecordEvent(screenTransitionEvent);
            Debug.Log($"Evento 'screenTransition' enviado - De: {fromScreen} a: {toScreen}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando screenTransition: {e.Message}");
        }
    }

    private bool CanRecordEvent()
    {
        if (!isInitialized)
        {
            Debug.Log("Analytics no está inicializado");
            return false;
        }

        if (!isDataCollectionActive)
        {
            Debug.Log("Recolección de datos no está activa");
            return false;
        }

        return true;
    }

    public void StopDataCollection()
    {
        if (isInitialized && AnalyticsService.Instance != null)
        {
            AnalyticsService.Instance.StopDataCollection();
            isDataCollectionActive = false;
            Debug.Log("Recolección de datos detenida");
        }
    }

    public void StartDataCollection()
    {
        if (isInitialized && AnalyticsService.Instance != null)
        {
            AnalyticsService.Instance.StartDataCollection();
            isDataCollectionActive = true;
            Debug.Log("Recolección de datos reiniciada");
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            RecordGameEnd(Time.time);
        }
        else
        {
            RecordGameStart();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            RecordGameEnd(Time.time);
        }
    }
}