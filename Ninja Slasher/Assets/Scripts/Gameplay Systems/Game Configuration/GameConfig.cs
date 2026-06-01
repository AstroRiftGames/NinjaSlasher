using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Game Configuration", order = 0)]
public class GameConfig : ScriptableObject
{
    [Header("LIVES SYSTEM")]

    [Tooltip("Maximo de vidas que puede tener el jugador")]
    public int maxLives = 5;
    [Tooltip("Vidas iniciales al empezar el juego")]
    public int startingLives = 5;
    [Tooltip("Segundos para regenerar 1 vida (1800 = 30 minutos)")]
    public int lifeRechargeSeconds = 1800;

    [Header("PROGRESSION SYSTEM")]
    [Tooltip("Niveles por area (normalmente 10)")]
    public int levelsPerArea = 10;
    [Tooltip("Total de areas en el juego")]
    public int totalAreas = 5;
    [Tooltip("Estrellas requeridas para desbloquear cada boss (indice 0 = boss area 1)")]
    public int[] starsRequiredPerBoss = { 5, 15, 30, 50, 75 };

    [Header("POWER-UPS - CANTIDADES OTORGADAS POR COMPRA")]
    [Tooltip("Cuantas unidades otorga cada compra en tienda de monedas")]
    [Range(1, 10)] public int extraTimeGrantQuantity = 3;
    [Range(1, 10)] public int dashTurboGrantQuantity = 5;
    [Range(1, 10)] public int parryPerfectGrantQuantity = 3;
    [Range(1, 10)] public int comboMasterGrantQuantity = 4;
    [Range(1, 5)]  public int secondChanceGrantQuantity = 1;
    [Range(1, 10)] public int enhancedParryGrantQuantity = 3;
    [Range(1, 10)] public int hawkVisionGrantQuantity = 1;

    [Header("POWER-UPS - COSTOS")]
    [Tooltip("Costo en monedas para comprar Extra Time")]
    public int extraTimeCost = 100;

    [Tooltip("Costo en monedas para comprar Dash Turbo")]
    public int dashTurboCost = 100;

    [Tooltip("Costo en monedas para comprar Parry Perfect")]
    public int parryPerfectCost = 100;

    [Tooltip("Costo en monedas para comprar Combo Master")]
    public int comboMasterCost = 100;

    [Tooltip("Costo en monedas para comprar Second Chance")]
    public int secondChanceCost = 100;

    [Tooltip("Costo en monedas para comprar Hawk Vision")]
    public int hawkVisionCost = 100;

    [Tooltip("Costo en monedas para comprar Enhanced Parry")]
    public int enhancedParryCost = 100;

    [Header("POWER-UPS - EFECTIVIDAD")]

    [Range(0.1f, 2f)]
    [Tooltip("Extra Time: Porcentaje de tiempo bonus (0.5 = 50% mas tiempo)")]
    public float extraTimeBonus = 0.5f;

    [Range(0.1f, 1f)]
    [Tooltip("Dash Turbo: Multiplicador de cooldown (0.25 = cooldown reducido a 25%)")]
    public float dashTurboCooldownMultiplier = 0.25f;

    [Range(0f, 0.5f)]
    [Tooltip("Parry Perfect: Distancia extra de parry (0.3 = 30% mas rango)")]
    public float parryPerfectBonusWindow = 0.3f;

    [Range(0.1f, 2f)]
    [Tooltip("Combo Master: Porcentaje de tiempo extra por nivel de combo (0.5 = 50% mas tiempo)")]
    public float comboMasterBonusPercent = 0.5f;

    [Range(2, 10)]
    [Tooltip("Enhanced Parry: Cantidad de rebotes")]
    public int enhancedParryBounces = 2;

    [Range(0.5f, 1f)]
    [Tooltip("Enhanced Parry: Retencion de velocidad por rebote")]
    public float enhancedParryVelocityRetention = 0.9f;

    [Header("MONETIZATION - ADS")]
    [Tooltip("Perdidas consecutivas necesarias para mostrar ad de vida extra")]
    public int lossesRequiredForAd = 2;
    [Tooltip("Niveles consecutivos completados para mostrar ad")]
    public int levelsRequiredForAd = 3;
    [Tooltip("Partidas jugadas en la sesion actual para mostrar un interstitial automatico")]
    public int gamesRequiredForInterstitialAd = 3;
    [Tooltip("Habilitar el contador de interstitials automaticos por partidas de la sesion")]
    public bool enableSessionGameplayInterstitialAds = true;
    [Tooltip("Habilitar ads despues de perdidas consecutivas")]
    public bool enableConsecutiveLossAds = true;
    [Tooltip("Habilitar ads despues de victorias consecutivas")]
    public bool enableConsecutiveLevelAds = true;
    [Tooltip("Habilitar ads cuando se queda sin vidas")]
    public bool enableNoLivesAds = true;
    [Tooltip("Habilitar ads al desbloquear nueva area")]
    public bool enableAreaUnlockAds = true;

    [Header("DAILY SYSTEMS")]
    [Tooltip("Dias que dura la semana de recompensas diarias")]
    public int dailyRewardWeekLength = 7;
    [Tooltip("Cantidad de recompensas en la ruleta diaria")]
    public int dailyWheelRewardCount = 8;

    [Header("COMBAT SYSTEM")]
    [Tooltip("Tiempo maximo entre kills para mantener combo (segundos)")]
    public float comboTimeWindow = 3f;
    [Tooltip("Nivel maximo de combo alcanzable")]
    public int maxComboLevel = 5;
    [Tooltip("Ventana de tiempo para hacer parry exitoso (segundos)")]
    public float parryWindow = 0.3f;
    [Tooltip("Multiplicador de daño por parry perfecto")]
    public float parryDamageMultiplier = 2f;

    [Header("ECONOMY")]
    [Tooltip("Monedas iniciales al empezar el juego")]
    public int initialCoins = 0;
    [Tooltip("Monedas ganadas por cada estrella obtenida")]
    public float coinsPerStar = 10f;
    [Tooltip("Monedas ganadas por completar un nivel")]
    public int coinsPerLevelCompleted = 50;

    [Header("LEVEL TIMING")]
    [Tooltip("Duracion por defecto de un nivel (segundos)")]
    public float defaultLevelDuration = 60f;
    [Tooltip("Tiempo minimo para obtener 3 estrellas (segundos)")]
    public float threeStarTimeThreshold = 30f;
    [Tooltip("Tiempo minimo para obtener 2 estrellas (segundos)")]
    public float twoStarTimeThreshold = 45f;

    [Header("DEBUG & TESTING")]
    [Tooltip("Iniciar con todos los niveles desbloqueados (solo desarrollo)")]
    public bool unlockAllLevelsOnStart = false;
    [Tooltip("Vidas infinitas (solo desarrollo)")]
    public bool infiniteLives = false;

    [Header("PERFORMANCE")]
    [Tooltip("Tamaño inicial del pool de proyectiles por tipo")]
    public int projectilePoolInitialSize = 20;
    [Tooltip("Tamaño inicial del pool de textos flotantes")]
    public int floatingTextPoolSize = 10;
    [Tooltip("Tamaño inicial del pool de efectos de particulas")]
    public int vfxPoolInitialSize = 15;

    [Header("NOTIFICATIONS")]
    [Tooltip("Habilitar notificacion local de vidas completas")]
    public bool enableLifeFullNotification = true;
    [Tooltip("ID del canal de notificaciones Android")]
    public string notificationAndroidChannelId = "retention_channel";
    [Tooltip("Nombre visible del canal de notificaciones (visible en ajustes Android)")]
    public string notificationAndroidChannelName = "Recordatorios";
    [Tooltip("Descripcion del canal de notificaciones")]
    public string notificationAndroidChannelDescription = "Recordatorios del juego";
    [Tooltip("Titulo de la notificacion de vidas completas")]
    public string lifeFullNotificationTitle = "Vidas Completas";
    [Tooltip("Cuerpo de la notificacion de vidas completas")]
    public string lifeFullNotificationBody = "Tus vidas estan al maximo. Vuelve a jugar!";
    [Tooltip("Habilitar notificacion local de recompensa diaria disponible")]
    public bool enableDailyRewardNotification = true;
    [Tooltip("Titulo de la notificacion de recompensa diaria")]
    public string dailyRewardNotificationTitle = "Recompensa Diaria";
    [Tooltip("Cuerpo de la notificacion de recompensa diaria")]
    public string dailyRewardNotificationBody = "Tu recompensa diaria esta lista. Vuelve para reclamarla!";
    [Tooltip("Usar demora corta de QA para la notificacion diaria. Mantener apagado en produccion.")]
    public bool useDebugDailyRewardNotificationDelay = false;
    [Tooltip("Demora de QA en segundos para la notificacion diaria")]
    public int debugDailyRewardNotificationDelaySeconds = 120;
    [Tooltip("Logs de depuracion del sistema de notificaciones")]
    public bool notificationDebugLogs = false;

    [Header("TUTORIAL")]
    [Tooltip("Mostrar tutorial en el primer nivel")]
    public bool enableTutorial = true;
    [Tooltip("Tiempo de visualizaciion de cada texto de tutorial (segundos)")]
    public float tutorialTextDisplayTime = 5f;


    public int GetStarsRequiredForBoss(int bossIndex)
    {
        if (bossIndex < 0 || bossIndex >= starsRequiredPerBoss.Length)
        {
            return 999;
        }
        return starsRequiredPerBoss[bossIndex];
    }

    public int CalculateStarsByTime(float completionTime)
    {
        if (completionTime <= threeStarTimeThreshold)
            return 3;
        if (completionTime <= twoStarTimeThreshold)
            return 2;
        return 1;
    }

    public int GetPowerUpGrantQuantity(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.ExtraTime: return extraTimeGrantQuantity;
            case PowerUpType.DashTurbo: return dashTurboGrantQuantity;
            case PowerUpType.ParryPerfect: return parryPerfectGrantQuantity;
            case PowerUpType.ComboMaster: return comboMasterGrantQuantity;
            case PowerUpType.SecondChance: return secondChanceGrantQuantity;
            case PowerUpType.HawkVision: return hawkVisionGrantQuantity;
            case PowerUpType.EnhancedParry: return enhancedParryGrantQuantity;
            default: return 1;
        }
    }

    public int GetPowerUpCost(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.ExtraTime: return extraTimeCost;
            case PowerUpType.DashTurbo: return dashTurboCost;
            case PowerUpType.ParryPerfect: return parryPerfectCost;
            case PowerUpType.ComboMaster: return comboMasterCost;
            case PowerUpType.SecondChance: return secondChanceCost;
            case PowerUpType.HawkVision: return hawkVisionCost;
            case PowerUpType.EnhancedParry: return enhancedParryCost;
            default: return 100;
        }
    }

    public float GetExtraTimeBonus()
    {
        return extraTimeBonus;
    }

    public float GetDashTurboCooldownMultiplier()
    {
        return dashTurboCooldownMultiplier;
    }

    public float GetParryPerfectBonusWindow()
    {
        return parryPerfectBonusWindow;
    }

    public float GetComboMasterBonusPercent()
    {
        return comboMasterBonusPercent;
    }

    public int GetEnhancedParryBounces()
    {
        return enhancedParryBounces;
    }

    public float GetEnhancedParryVelocityRetention()
    {
        return enhancedParryVelocityRetention;
    }

    public bool ValidateConfiguration()
    {
        bool isValid = true;

        if (maxLives <= 0)
        {
            Debug.LogError("[GameConfig] maxLives debe ser mayor a 0");
            isValid = false;
        }

        if (startingLives > maxLives)
        {
            Debug.LogError("[GameConfig] startingLives no puede ser mayor a maxLives");
            isValid = false;
        }

        if (levelsPerArea <= 0)
        {
            Debug.LogError("[GameConfig] levelsPerArea debe ser mayor a 0");
            isValid = false;
        }

        if (starsRequiredPerBoss.Length != totalAreas)
        {
            Debug.LogError($"[GameConfig] starsRequiredPerBoss debe tener {totalAreas} elementos");
            isValid = false;
        }

        if (comboTimeWindow <= 0)
        {
            Debug.LogError("[GameConfig] comboTimeWindow debe ser mayor a 0");
            isValid = false;
        }

        return isValid;
    }

#if UNITY_EDITOR
    [ContextMenu("Validate Configuration")]
    private void ValidateInEditor()
    {
        if (ValidateConfiguration())
        {
            Debug.Log("[GameConfig] Configuraci�n v�lida");
        }
        else
        {
            Debug.LogError("[GameConfig] Configuraci�n inv�lida - revisa los errores arriba");
        }
    }

    [ContextMenu("Print Configuration Summary")]
    private void PrintSummary()
    {
        Debug.Log("========== GAME CONFIGURATION ==========");
        Debug.Log($"Lives: {startingLives}/{maxLives} (recharge: {lifeRechargeSeconds}s)");
        Debug.Log($"Progression: {totalAreas} areas � {levelsPerArea} levels = {totalAreas * levelsPerArea} total");
        Debug.Log($"Combat: Combo window {comboTimeWindow}s, Max combo {maxComboLevel}");
        Debug.Log($"Economy: {coinsPerStar} coins/star, {coinsPerLevelCompleted} coins/level");
        Debug.Log("========================================");
    }
#endif
}
