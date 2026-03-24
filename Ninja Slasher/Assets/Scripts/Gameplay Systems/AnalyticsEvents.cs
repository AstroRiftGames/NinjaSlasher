/// <summary>
/// Centraliza los nombres de eventos y propiedades de analytics.
/// Usar siempre estas constantes en lugar de strings literales para evitar
/// typos silenciosos que crean eventos fantasma en el backend.
/// </summary>
public static class AnalyticsEvents
{
    // Nombres de eventos
    public const string GameStart        = "gameStart";
    public const string GameEnd          = "gameEnd";
    public const string LevelStart       = "levelStart";
    public const string LevelCompleted   = "levelCompleted";
    public const string LevelFailed      = "levelFailed";
    public const string LifeLost            = "lifeLost";
    public const string LifeRestored        = "lifeRestored";
    public const string LifeWallEncountered = "lifeWallEncountered";
    public const string LifeWallResolved    = "lifeWallResolved";
    public const string ScreenTransition    = "screenTransition";
    public const string ShopOpened          = "shopOpened";
    public const string ProductSelected     = "productSelected";
    public const string PurchaseStarted     = "purchaseStarted";
    public const string PurchaseCompleted   = "purchaseCompleted";
    public const string PurchaseFailed      = "purchaseFailed";
    public const string PurchaseRestored    = "purchaseRestored";
    public const string PaywallShown        = "paywallShown";
    public const string PaywallResolved     = "paywallResolved";

    public const string InterstitialOpportunity = "interstitialOpportunity";
    public const string InterstitialShown       = "interstitialShown";
    public const string InterstitialFailed      = "interstitialFailed";
    public const string InterstitialClosed      = "interstitialClosed";

    public const string FirstOpen            = "firstOpen";
    public const string TutorialStarted     = "tutorialStarted";
    public const string TutorialStepStarted = "tutorialStepStarted";
    public const string TutorialStepCompleted = "tutorialStepCompleted";
    public const string TutorialCompleted   = "tutorialCompleted";
    public const string TutorialAbandoned   = "tutorialAbandoned";

    public const string RewardedAdRequested = "rewardedAdRequested";
    public const string RewardedAdShown     = "rewardedAdShown";
    public const string RewardedAdCompleted = "rewardedAdCompleted";
    public const string RewardedAdFailed    = "rewardedAdFailed";
    public const string RewardedAdClosed    = "rewardedAdClosed";

    public const string PowerUpActivated      = "powerUpActivated";
    public const string PowerUpExpired        = "powerUpExpired";
    public const string LevelMechanicsSummary = "levelMechanicsSummary";

    // Nombres de propiedades de payload
    public static class Props
    {
        public const string SessionId      = "sessionId";
        public const string EventTime      = "eventTime";
        public const string DevicePlatform = "devicePlatform";
        public const string Version        = "version";
        public const string SessionDuration = "sessionDuration";

        public const string LevelId        = "levelId";
        public const string StarsEarned    = "starsEarned";
        public const string CompletionTime = "completionTime";
        public const string FailReason     = "failReason";
        public const string AttemptTime    = "attemptTime";
        public const string AttemptNumber  = "attemptNumber";

        public const string CurrentLives   = "currentLives";
        public const string TotalLivesLost = "totalLivesLost";
        public const string LossReason     = "lossReason";
        public const string NewLifeCount   = "newLifeCount";
        public const string RestoreMethod  = "restoreMethod";

        public const string FromScreen     = "fromScreen";
        public const string ToScreen       = "toScreen";

        public const string Outcome           = "outcome";
        public const string Context           = "context";
        public const string Reason            = "reason";

        public const string ProductId         = "productId";
        public const string Category          = "category";
        public const string Source            = "source";
        public const string TransactionId     = "transactionId";
        public const string Tier              = "tier";
        public const string ConsecutiveLosses = "consecutiveLosses";
        public const string IsBossLevel       = "isBossLevel";

        public const string Placement     = "placement";

        public const string TutorialId    = "tutorialId";
        public const string StepId        = "stepId";
        public const string StepIndex     = "stepIndex";
        public const string TotalSteps    = "totalSteps";
        public const string AbandonReason = "abandonReason";

        public const string PowerUpType          = "powerUpType";
        public const string UsesGranted          = "usesGranted";
        public const string MovesUsed            = "movesUsed";
        public const string MaxComboLevel        = "maxComboLevel";
        public const string EnemiesDefeated      = "enemiesDefeated";
        public const string TotalEnemies         = "totalEnemies";
        public const string ReflectedKills       = "reflectedKills";
        public const string MaxSingleAttackKills = "maxSingleAttackKills";
        public const string ActivePowerUps       = "activePowerUps";
        public const string ActivatedAtLevelId   = "activatedAtLevelId";
    }
}
