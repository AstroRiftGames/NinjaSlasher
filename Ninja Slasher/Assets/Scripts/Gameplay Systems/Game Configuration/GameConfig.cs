using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Game Configuration", order = 0)]
public class GameConfig : ScriptableObject
{
    [Header("LIVES SYSTEM")]

    [Tooltip("Máximo de vidas que puede tener el jugador")]
    public int maxLives = 5;
    [Tooltip("Vidas iniciales al empezar el juego")]
    public int startingLives = 5;
    [Tooltip("Segundos para regenerar 1 vida (1800 = 30 minutos)")]
    public int lifeRechargeSeconds = 1800;

    [Header("PROGRESSION SYSTEM")]
    [Tooltip("Niveles por área (normalmente 10)")]
    public int levelsPerArea = 10;
    [Tooltip("Total de áreas en el juego")]
    public int totalAreas = 5;
    [Tooltip("Estrellas requeridas para desbloquear cada boss (índice 0 = boss área 1)")]
    public int[] starsRequiredPerBoss = { 5, 15, 30, 50, 75 };

    [Header("MONETIZATION - ADS")]
    [Tooltip("Pérdidas consecutivas necesarias para mostrar ad de vida extra")]
    public int lossesRequiredForAd = 2;
    [Tooltip("Niveles consecutivos completados para mostrar ad")]
    public int levelsRequiredForAd = 3;
    [Tooltip("Habilitar ads después de pérdidas consecutivas")]
    public bool enableConsecutiveLossAds = true;
    [Tooltip("Habilitar ads después de victorias consecutivas")]
    public bool enableConsecutiveLevelAds = true;
    [Tooltip("Habilitar ads cuando se queda sin vidas")]
    public bool enableNoLivesAds = true;
    [Tooltip("Habilitar ads al desbloquear nueva área")]
    public bool enableAreaUnlockAds = true;

    [Header("DAILY SYSTEMS")]
    [Tooltip("Días que dura la semana de recompensas diarias")]
    public int dailyRewardWeekLength = 7;
    [Tooltip("Cantidad de recompensas en la ruleta diaria")]
    public int dailyWheelRewardCount = 8;

    [Header("COMBAT SYSTEM")]
    [Tooltip("Tiempo máximo entre kills para mantener combo (segundos)")]
    public float comboTimeWindow = 3f;
    [Tooltip("Nivel máximo de combo alcanzable")]
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
    [Tooltip("Duración por defecto de un nivel (segundos)")]
    public float defaultLevelDuration = 60f;
    [Tooltip("Tiempo mínimo para obtener 3 estrellas (segundos)")]
    public float threeStarTimeThreshold = 30f;
    [Tooltip("Tiempo mínimo para obtener 2 estrellas (segundos)")]
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
    [Tooltip("Tamaño inicial del pool de efectos de partículas")]
    public int vfxPoolInitialSize = 15;

    [Header("TUTORIAL")]
    [Tooltip("Mostrar tutorial en el primer nivel")]
    public bool enableTutorial = true;
    [Tooltip("Tiempo de visualización de cada texto de tutorial (segundos)")]
    public float tutorialTextDisplayTime = 5f;

    public int GetStarsRequiredForBoss(int bossIndex)
    {
        if (bossIndex < 0 || bossIndex >= starsRequiredPerBoss.Length)
        {
            Debug.LogWarning($"[GameConfig] Boss index {bossIndex} fuera de rango");
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
            Debug.Log("[GameConfig] Configuración válida");
        }
        else
        {
            Debug.LogError("[GameConfig] Configuración inválida - revisa los errores arriba");
        }
    }

    [ContextMenu("Print Configuration Summary")]
    private void PrintSummary()
    {
        Debug.Log("========== GAME CONFIGURATION ==========");
        Debug.Log($"Lives: {startingLives}/{maxLives} (recharge: {lifeRechargeSeconds}s)");
        Debug.Log($"Progression: {totalAreas} areas × {levelsPerArea} levels = {totalAreas * levelsPerArea} total");
        Debug.Log($"Combat: Combo window {comboTimeWindow}s, Max combo {maxComboLevel}");
        Debug.Log($"Economy: {coinsPerStar} coins/star, {coinsPerLevelCompleted} coins/level");
        Debug.Log("========================================");
    }
#endif
}