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

    private DateTime _sessionStartUtc = DateTime.UtcNow;

    private bool _gameEndFired = false;

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
        if (!enableAnalyticsInEditor && Application.isEditor)
        {
            return;
        }

        try
        {
            await UnityServices.InitializeAsync();

            if (AnalyticsService.Instance != null)
            {
                AnalyticsService.Instance.StartDataCollection();

                isInitialized = true;
                isDataCollectionActive = true;

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

        _sessionStartUtc = DateTime.UtcNow;
        _gameEndFired = false;

        try
        {
            var gameStartEvent = new CustomEvent(AnalyticsEvents.GameStart)
            {
                { AnalyticsEvents.Props.SessionId,      Guid.NewGuid().ToString() },
                { AnalyticsEvents.Props.EventTime,      GetEventTime() },
                { AnalyticsEvents.Props.DevicePlatform, Application.platform.ToString() },
                { AnalyticsEvents.Props.Version,        Application.version }
            };

            AnalyticsService.Instance.RecordEvent(gameStartEvent);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando gameStart: {e.Message}");
        }

        RecordFirstOpen();
    }

    private void RecordFirstOpen()
    {
        if (!CanRecordEvent()) return;
        if (SaveManager.Instance == null) return;

        var data = SaveManager.Instance.GetGameData();
        if (data.hasFirstOpenFired) return;

        SaveManager.Instance.Modify(d => d.hasFirstOpenFired = true);

        try
        {
            var e = new CustomEvent(AnalyticsEvents.FirstOpen)
            {
                { AnalyticsEvents.Props.DevicePlatform, Application.platform.ToString() },
                { AnalyticsEvents.Props.Version,        Application.version },
                { AnalyticsEvents.Props.EventTime,      GetEventTime() }
            };
            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando firstOpen: {e.Message}");
        }
    }

    public void RecordGameEnd()
    {
        if (_gameEndFired) return;

        if (!CanRecordEvent()) return;

        _gameEndFired = true;
        
        float sessionDuration = (float)(DateTime.UtcNow - _sessionStartUtc).TotalSeconds;
        if (sessionDuration < 0f) sessionDuration = 0f;

        try
        {
            var gameEndEvent = new CustomEvent(AnalyticsEvents.GameEnd)
            {
                { AnalyticsEvents.Props.SessionDuration, sessionDuration },
                { AnalyticsEvents.Props.EventTime,       GetEventTime() },
                { AnalyticsEvents.Props.DevicePlatform,  Application.platform.ToString() }
            };

            AnalyticsService.Instance.RecordEvent(gameEndEvent);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando gameEnd: {e.Message}");
        }
    }

    public void RecordLevelStart(int levelId, string activePowerUps = "")
    {
        if (!CanRecordEvent()) return;

        try
        {
            var levelStartEvent = new CustomEvent(AnalyticsEvents.LevelStart)
            {
                { AnalyticsEvents.Props.LevelId,       levelId },
                { AnalyticsEvents.Props.ActivePowerUps, activePowerUps },
                { AnalyticsEvents.Props.EventTime,      GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(levelStartEvent);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando levelStart: {e.Message}");
        }
    }

    public void RecordLevelCompleted(int levelId, int starsEarned, float completionTime)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var levelCompletedEvent = new CustomEvent(AnalyticsEvents.LevelCompleted)
            {
                { AnalyticsEvents.Props.LevelId,        levelId },
                { AnalyticsEvents.Props.StarsEarned,    starsEarned },
                { AnalyticsEvents.Props.CompletionTime, completionTime },
                { AnalyticsEvents.Props.EventTime,      GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(levelCompletedEvent);
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
            var levelFailedEvent = new CustomEvent(AnalyticsEvents.LevelFailed)
            {
                { AnalyticsEvents.Props.LevelId,       levelId },
                { AnalyticsEvents.Props.FailReason,    failReason },
                { AnalyticsEvents.Props.AttemptTime,   attemptTime },
                { AnalyticsEvents.Props.AttemptNumber, attemptNumber },
                { AnalyticsEvents.Props.EventTime,     GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(levelFailedEvent);
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
            var lifeLostEvent = new CustomEvent(AnalyticsEvents.LifeLost)
            {
                { AnalyticsEvents.Props.CurrentLives,   currentLives },
                { AnalyticsEvents.Props.TotalLivesLost, totalLivesLost },
                { AnalyticsEvents.Props.LossReason,     lossReason },
                { AnalyticsEvents.Props.EventTime,      GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(lifeLostEvent);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando lifeLost: {e.Message}");
        }
    }

    public void RecordLifeRestored(int newLifeCount, LifeRestoreSource source)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var lifeRestoredEvent = new CustomEvent(AnalyticsEvents.LifeRestored)
            {
                { AnalyticsEvents.Props.NewLifeCount,  newLifeCount },
                { AnalyticsEvents.Props.RestoreMethod, LifeRestoreSourceToString(source) },
                { AnalyticsEvents.Props.EventTime,     GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(lifeRestoredEvent);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando lifeRestored: {e.Message}");
        }
    }

    public void RecordShopOpened()
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.ShopOpened)
            {
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando shopOpened: {e.Message}");
        }
    }

    public void RecordProductSelected(string productId, string category)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.ProductSelected)
            {
                { AnalyticsEvents.Props.ProductId, productId },
                { AnalyticsEvents.Props.Category,  category },
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando productSelected: {e.Message}");
        }
    }

    public void RecordPurchaseStarted(string productId, string category, string source)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.PurchaseStarted)
            {
                { AnalyticsEvents.Props.ProductId, productId },
                { AnalyticsEvents.Props.Category,  category },
                { AnalyticsEvents.Props.Source,    source },
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando purchaseStarted: {e.Message}");
        }
    }

    public void RecordPurchaseCompleted(string productId, string category, string source, string transactionId)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.PurchaseCompleted)
            {
                { AnalyticsEvents.Props.ProductId,     productId },
                { AnalyticsEvents.Props.Category,      category },
                { AnalyticsEvents.Props.Source,        source },
                { AnalyticsEvents.Props.TransactionId, transactionId },
                { AnalyticsEvents.Props.EventTime,     GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando purchaseCompleted: {e.Message}");
        }
    }

    public void RecordPurchaseFailed(string productId, string reason)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.PurchaseFailed)
            {
                { AnalyticsEvents.Props.ProductId, productId },
                { AnalyticsEvents.Props.Reason,    reason },
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando purchaseFailed: {e.Message}");
        }
    }

    public void RecordPurchaseRestored()
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.PurchaseRestored)
            {
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando purchaseRestored: {e.Message}");
        }
    }

    public void RecordPaywallShown(string productId, string tier, int levelId, int consecutiveLosses, bool isBossLevel)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.PaywallShown)
            {
                { AnalyticsEvents.Props.ProductId,         productId },
                { AnalyticsEvents.Props.Tier,              tier },
                { AnalyticsEvents.Props.LevelId,           levelId },
                { AnalyticsEvents.Props.ConsecutiveLosses, consecutiveLosses },
                { AnalyticsEvents.Props.IsBossLevel,       isBossLevel },
                { AnalyticsEvents.Props.EventTime,         GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando paywallShown: {e.Message}");
        }
    }

    public void RecordPaywallResolved(PaywallOutcome outcome, string productId, string tier)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.PaywallResolved)
            {
                { AnalyticsEvents.Props.Outcome,   PaywallOutcomeToString(outcome) },
                { AnalyticsEvents.Props.ProductId, productId },
                { AnalyticsEvents.Props.Tier,      tier },
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando paywallResolved: {e.Message}");
        }
    }

    public void RecordLifeWallEncountered(int levelId)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.LifeWallEncountered)
            {
                { AnalyticsEvents.Props.LevelId,   levelId },
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando lifeWallEncountered: {e.Message}");
        }
    }

    public void RecordLifeWallResolved(LifeWallOutcome outcome, int levelId)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.LifeWallResolved)
            {
                { AnalyticsEvents.Props.Outcome,   LifeWallOutcomeToString(outcome) },
                { AnalyticsEvents.Props.LevelId,   levelId },
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando lifeWallResolved: {e.Message}");
        }
    }

    public void RecordRewardedAdRequested(string context)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.RewardedAdRequested)
            {
                { AnalyticsEvents.Props.Context,   context },
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando rewardedAdRequested: {e.Message}");
        }
    }

    public void RecordRewardedAdShown(string context)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.RewardedAdShown)
            {
                { AnalyticsEvents.Props.Context,   context },
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando rewardedAdShown: {e.Message}");
        }
    }

    public void RecordRewardedAdCompleted(string context)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.RewardedAdCompleted)
            {
                { AnalyticsEvents.Props.Context,   context },
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando rewardedAdCompleted: {e.Message}");
        }
    }

    public void RecordRewardedAdFailed(string context, string reason)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.RewardedAdFailed)
            {
                { AnalyticsEvents.Props.Context,   context },
                { AnalyticsEvents.Props.Reason,    reason },
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando rewardedAdFailed: {e.Message}");
        }
    }

    public void RecordRewardedAdClosed(string context)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.RewardedAdClosed)
            {
                { AnalyticsEvents.Props.Context,   context },
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando rewardedAdClosed: {e.Message}");
        }
    }

    public void RecordInterstitialOpportunity(string placement, int levelId = -1)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.InterstitialOpportunity)
            {
                { AnalyticsEvents.Props.Placement, placement },
                { AnalyticsEvents.Props.LevelId,   levelId },
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };
            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando interstitialOpportunity: {e.Message}");
        }
    }

    public void RecordInterstitialShown(string placement, int levelId = -1)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.InterstitialShown)
            {
                { AnalyticsEvents.Props.Placement, placement },
                { AnalyticsEvents.Props.LevelId,   levelId },
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };
            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando interstitialShown: {e.Message}");
        }
    }

    public void RecordInterstitialFailed(string placement, string reason, int levelId = -1)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.InterstitialFailed)
            {
                { AnalyticsEvents.Props.Placement, placement },
                { AnalyticsEvents.Props.Reason,    reason },
                { AnalyticsEvents.Props.LevelId,   levelId },
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };
            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando interstitialFailed: {e.Message}");
        }
    }

    public void RecordInterstitialClosed(string placement, int levelId = -1)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.InterstitialClosed)
            {
                { AnalyticsEvents.Props.Placement, placement },
                { AnalyticsEvents.Props.LevelId,   levelId },
                { AnalyticsEvents.Props.EventTime, GetEventTime() }
            };
            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando interstitialClosed: {e.Message}");
        }
    }

    public void RecordTutorialStarted(string tutorialId, int totalSteps)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.TutorialStarted)
            {
                { AnalyticsEvents.Props.TutorialId, tutorialId },
                { AnalyticsEvents.Props.TotalSteps, totalSteps },
                { AnalyticsEvents.Props.EventTime,  GetEventTime() }
            };
            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando tutorialStarted: {e.Message}");
        }
    }

    public void RecordTutorialStepStarted(string tutorialId, string stepId, int stepIndex, int totalSteps)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.TutorialStepStarted)
            {
                { AnalyticsEvents.Props.TutorialId, tutorialId },
                { AnalyticsEvents.Props.StepId,     stepId },
                { AnalyticsEvents.Props.StepIndex,  stepIndex },
                { AnalyticsEvents.Props.TotalSteps, totalSteps },
                { AnalyticsEvents.Props.EventTime,  GetEventTime() }
            };
            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando tutorialStepStarted: {e.Message}");
        }
    }

    public void RecordTutorialStepCompleted(string tutorialId, string stepId, int stepIndex, int totalSteps)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.TutorialStepCompleted)
            {
                { AnalyticsEvents.Props.TutorialId, tutorialId },
                { AnalyticsEvents.Props.StepId,     stepId },
                { AnalyticsEvents.Props.StepIndex,  stepIndex },
                { AnalyticsEvents.Props.TotalSteps, totalSteps },
                { AnalyticsEvents.Props.EventTime,  GetEventTime() }
            };
            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando tutorialStepCompleted: {e.Message}");
        }
    }

    public void RecordTutorialCompleted(string tutorialId, int totalSteps)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.TutorialCompleted)
            {
                { AnalyticsEvents.Props.TutorialId, tutorialId },
                { AnalyticsEvents.Props.TotalSteps, totalSteps },
                { AnalyticsEvents.Props.EventTime,  GetEventTime() }
            };
            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando tutorialCompleted: {e.Message}");
        }
    }

    public void RecordTutorialAbandoned(string tutorialId, string reason, string stepId, int stepIndex, int totalSteps)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.TutorialAbandoned)
            {
                { AnalyticsEvents.Props.TutorialId,    tutorialId },
                { AnalyticsEvents.Props.AbandonReason, reason },
                { AnalyticsEvents.Props.StepId,        stepId },
                { AnalyticsEvents.Props.StepIndex,     stepIndex },
                { AnalyticsEvents.Props.TotalSteps,    totalSteps },
                { AnalyticsEvents.Props.EventTime,     GetEventTime() }
            };
            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando tutorialAbandoned: {e.Message}");
        }
    }

    public void RecordPowerUpActivated(string powerUpType, int usesGranted, int levelId, int attemptNumber)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.PowerUpActivated)
            {
                { AnalyticsEvents.Props.PowerUpType,   powerUpType },
                { AnalyticsEvents.Props.UsesGranted,   usesGranted },
                { AnalyticsEvents.Props.LevelId,       levelId },
                { AnalyticsEvents.Props.AttemptNumber, attemptNumber },
                { AnalyticsEvents.Props.EventTime,     GetEventTime() }
            };
            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando powerUpActivated: {e.Message}");
        }
    }

    public void RecordPowerUpExpired(string powerUpType, int levelId, int activatedAtLevelId, int attemptNumber)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.PowerUpExpired)
            {
                { AnalyticsEvents.Props.PowerUpType,        powerUpType },
                { AnalyticsEvents.Props.LevelId,            levelId },
                { AnalyticsEvents.Props.ActivatedAtLevelId, activatedAtLevelId },
                { AnalyticsEvents.Props.AttemptNumber,      attemptNumber },
                { AnalyticsEvents.Props.EventTime,          GetEventTime() }
            };
            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando powerUpExpired: {e.Message}");
        }
    }

    public void RecordLevelMechanicsSummary(
        int levelId, string outcome, int attemptNumber,
        int movesUsed, int maxComboLevel, int enemiesDefeated,
        int totalEnemies, int reflectedKills, int maxSingleAttackKills,
        string activePowerUps = "")
    {
        if (!CanRecordEvent()) return;

        try
        {
            var e = new CustomEvent(AnalyticsEvents.LevelMechanicsSummary)
            {
                { AnalyticsEvents.Props.LevelId,             levelId },
                { AnalyticsEvents.Props.Outcome,             outcome },
                { AnalyticsEvents.Props.AttemptNumber,       attemptNumber },
                { AnalyticsEvents.Props.MovesUsed,           movesUsed },
                { AnalyticsEvents.Props.MaxComboLevel,       maxComboLevel },
                { AnalyticsEvents.Props.EnemiesDefeated,     enemiesDefeated },
                { AnalyticsEvents.Props.TotalEnemies,        totalEnemies },
                { AnalyticsEvents.Props.ReflectedKills,      reflectedKills },
                { AnalyticsEvents.Props.MaxSingleAttackKills, maxSingleAttackKills },
                { AnalyticsEvents.Props.ActivePowerUps,      activePowerUps },
                { AnalyticsEvents.Props.EventTime,           GetEventTime() }
            };
            AnalyticsService.Instance.RecordEvent(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando levelMechanicsSummary: {e.Message}");
        }
    }

    public void RecordScreenTransition(string fromScreen, string toScreen)
    {
        if (!CanRecordEvent()) return;

        try
        {
            var screenTransitionEvent = new CustomEvent(AnalyticsEvents.ScreenTransition)
            {
                { AnalyticsEvents.Props.FromScreen, fromScreen },
                { AnalyticsEvents.Props.ToScreen,   toScreen },
                { AnalyticsEvents.Props.EventTime,  GetEventTime() }
            };

            AnalyticsService.Instance.RecordEvent(screenTransitionEvent);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnalyticsManager] Error enviando screenTransition: {e.Message}");
        }
    }

    private bool CanRecordEvent()
    {
        if (!enableAnalyticsInEditor && Application.isEditor)
            return false;

        if (!isInitialized)
        {
            return false;
        }

        if (!isDataCollectionActive)
        {
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
        }
    }

    public void StartDataCollection()
    {
        if (isInitialized && AnalyticsService.Instance != null)
        {
            AnalyticsService.Instance.StartDataCollection();
            isDataCollectionActive = true;
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            RecordGameEnd();
        else
            RecordGameStart();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            RecordGameEnd();
    }

    private static string GetEventTime()
        => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

    private static string PaywallOutcomeToString(PaywallOutcome outcome) => outcome switch
    {
        PaywallOutcome.Purchased => "purchased",
        PaywallOutcome.Dismissed => "dismissed",
        PaywallOutcome.Expired   => "expired",
        _                        => "unknown"
    };

    public static string ProductCategoryStr(ProductCategory cat) => cat switch
    {
        ProductCategory.Bundle   => "bundle",
        ProductCategory.CoinPack => "coinPack",
        _                        => "unknown"
    };

    private static string LifeWallOutcomeToString(LifeWallOutcome outcome) => outcome switch
    {
        LifeWallOutcome.AdReward    => "adReward",
        LifeWallOutcome.IapPurchase => "iapPurchase",
        LifeWallOutcome.Waited      => "waited",
        LifeWallOutcome.Abandoned   => "abandoned",
        _                           => "unknown"
    };

    private static string LifeRestoreSourceToString(LifeRestoreSource source) => source switch
    {
        LifeRestoreSource.TimeRegeneration => "timeRegeneration",
        LifeRestoreSource.AdReward         => "adReward",
        LifeRestoreSource.IapPurchase      => "iapPurchase",
        LifeRestoreSource.DailyBonus       => "dailyBonus",
        LifeRestoreSource.DailyWheel       => "dailyWheel",
        LifeRestoreSource.EmergencyBundle  => "emergencyBundle",
        LifeRestoreSource.Debug            => "debug",
        _                                  => "unknown"
    };
}
