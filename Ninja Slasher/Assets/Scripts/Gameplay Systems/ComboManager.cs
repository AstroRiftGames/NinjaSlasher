using UnityEngine;

public class ComboManager : MonoBehaviourSingleton<ComboManager>
{
    private float ComboTimeWindow => GameConfigManager.Config.comboTimeWindow;
    private int MaxComboLevel => GameConfigManager.Config.maxComboLevel;

    private int killCount;
    private float comboTimer;
    private bool comboActive;
    private Vector3 lastEnemyPosition;

    private float _currentComboDuration;
    private float _maxComboDuration;
    private int _maxComboLevelReached;

    public float MaxComboDuration
    {
        get
        {
            return comboActive ? Mathf.Max(_maxComboDuration, _currentComboDuration) : _maxComboDuration;
        }
    }

    public int MaxComboLevelReached => _maxComboLevelReached;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private int _debugTotalKillsReceived;
#endif

    public override void Awake()
    {
        base.Awake();
    }

    private void OnEnable()
    {
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
        GameEvents.OnLevelSessionClosed += HandleLevelSessionClosed;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        GameEvents.OnLevelSessionClosed -= HandleLevelSessionClosed;
    }

    private void HandleEnemyKilled(Vector3 position)
    {
        if (!IsGameplaySessionRunning())
            return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _debugTotalKillsReceived++;
        Debug.Log($"[Combo] Kill #{_debugTotalKillsReceived} recibido | killCount antes: {killCount} -> combo proyectado: x{Mathf.Clamp(killCount + 1, 1, MaxComboLevel)}");
#endif

        RegisterKill(position);
    }

    private void Update()
    {
        if (!comboActive)
            return;

        comboTimer -= Time.deltaTime;
        _currentComboDuration += Time.deltaTime;

        if (comboTimer <= 0f)
        {
            ResetCombo();
        }
    }

    private void RegisterKill(Vector3 enemyPosition)
    {
        lastEnemyPosition = enemyPosition;

        if (!comboActive)
            _currentComboDuration = 0f;

        killCount++;
        int level = Mathf.Clamp(killCount, 1, MaxComboLevel);

        if (level > _maxComboLevelReached)
            _maxComboLevelReached = level;

        comboTimer = level switch
        {
            1 => ComboTimeWindow * 0.67f,
            2 => ComboTimeWindow * 0.53f,
            3 => ComboTimeWindow * 0.47f,
            4 => ComboTimeWindow * 0.40f,
            _ => ComboTimeWindow * 0.33f
        };

        comboActive = true;

        if (level < 2)
            return;

        GiveBonus(level);
        GameEvents.RaiseComboUpdated(level, lastEnemyPosition);
    }

    private void GiveBonus(int level)
    {
        float bonus = level switch
        {
            2 => 3f,
            3 => 4f,
            4 => 6f,
            _ => 3f
        };

        PowerUpContext context = PowerUpManager.Instance?.context;
        if (context != null && context.ComboMasterActive)
        {
            float percent = context.ComboBonusPercent;
            float bonusExtra = bonus * percent;
            bonus += bonusExtra;
        }

        GameEvents.RaiseLevelTimeBonus(bonus);
    }

    private void ResetCombo()
    {
        ClearComboState(notifyReset: true, resetSessionStats: false);
    }

    private void HandleLevelSessionClosed()
    {
        ClearComboState(notifyReset: false, resetSessionStats: true);
    }

    private void ClearComboState(bool notifyReset, bool resetSessionStats)
    {
        if (_currentComboDuration > _maxComboDuration)
            _maxComboDuration = _currentComboDuration;

        bool shouldNotifyReset = notifyReset && comboActive;

        _currentComboDuration = 0f;
        killCount = 0;
        comboTimer = 0f;
        comboActive = false;
        lastEnemyPosition = Vector3.zero;

        if (resetSessionStats)
        {
            _maxComboDuration = 0f;
            _maxComboLevelReached = 0;
        }

        if (shouldNotifyReset)
        {
            GameEvents.RaiseComboReset();
        }
    }

    private static bool IsGameplaySessionRunning()
    {
        return LevelSessionManager.Instance != null && LevelSessionManager.Instance.IsSessionRunning;
    }

    public Vector3 GetLastEnemyPosition()
    {
        return lastEnemyPosition;
    }

    public int GetCurrentComboLevel() => Mathf.Clamp(killCount, 1, MaxComboLevel);
    public float GetRemainingTime() => comboActive ? comboTimer : 0f;
    public bool IsComboActive() => comboActive;
}
