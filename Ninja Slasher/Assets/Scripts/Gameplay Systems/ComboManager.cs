using UnityEngine;
using System;

public class ComboManager : MonoBehaviourSingleton<ComboManager>
{
    [Obsolete]
    public event Action<int> OnComboUpdated;

    [Obsolete]
    public event Action OnComboEnded;

    [Obsolete]
    public event Action<int, Vector3> OnComboUpdatedWithPosition;

    private float ComboTimeWindow => GameConfigManager.Config.comboTimeWindow;
    private int MaxComboLevel => GameConfigManager.Config.maxComboLevel;

    private int killCount = 0;
    private float comboTimer = 0f;
    private bool comboActive = false;
    private Vector3 lastEnemyPosition;

    private float _currentComboDuration = 0f;
    private float _maxComboDuration = 0f;
    private int _maxComboLevelReached = 0;

    public float MaxComboDuration
    {
        get
        {
            return comboActive ? Mathf.Max(_maxComboDuration, _currentComboDuration) : _maxComboDuration;
        }
    }
    public int MaxComboLevelReached => _maxComboLevelReached;

    public override void Awake()
    {
        base.Awake();
    }

    void Update()
    {
        if (!comboActive) return;

        comboTimer -= Time.deltaTime;
        _currentComboDuration += Time.deltaTime;

        if (comboTimer <= 0f)
        {
            ResetCombo();
        }
    }

    public void RegisterKill(Vector3 enemyPosition)
    {
        lastEnemyPosition = enemyPosition;

        if (!comboActive)
            _currentComboDuration = 0f;

        killCount++;
        int level = Mathf.Clamp(killCount, 1, 5);

        if (level > _maxComboLevelReached)
            _maxComboLevelReached = level;

        comboTimer = level switch
        {
            1 => ComboTimeWindow * 0.67f,  // ~2s si base es 3s
            2 => ComboTimeWindow * 0.53f,  // ~1.6s
            3 => ComboTimeWindow * 0.47f,  // ~1.4s
            4 => ComboTimeWindow * 0.40f,  // ~1.2s
            _ => ComboTimeWindow * 0.33f   // ~1s
        };

        comboActive = true;

        if (level >= 2)
        {
            GiveBonus(level);

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnComboPerformed();
            }

            //Debug.Log($"[ComboManager] Combo x{level} activado en posici�n {lastEnemyPosition}");

            GameEvents.RaiseComboUpdated(level, lastEnemyPosition);
        }
    }

    public void RegisterKill()
    {
        RegisterKill(Vector3.zero);
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

        var context = PowerUpManager.Instance?.context;
        if (context != null && context.ComboMasterActive)
        {
            float percent = context.ComboBonusPercent;
            float bonusExtra = bonus * percent;
            bonus += bonusExtra;

            //Debug.Log($"[ComboManager] Bonus aumentado por ComboMaster: {bonus:F1}s (base + {bonusExtra:F1}s)");
        }

        GameEvents.RaiseLevelTimeBonus(bonus);

        //Debug.Log($"[ComboManager] Bonus de tiempo otorgado: +{bonus:F1}s");
    }

    private void ResetCombo()
    {
        if (killCount > 1)
        {
            //Debug.Log($"[ComboManager] Combo x{killCount} terminado");
        }

        if (_currentComboDuration > _maxComboDuration)
            _maxComboDuration = _currentComboDuration;

        _currentComboDuration = 0f;
        killCount = 0;
        comboActive = false;

        GameEvents.RaiseComboReset();
    }

    public Vector3 GetLastEnemyPosition()
    {
        return lastEnemyPosition;
    }

    public int GetCurrentComboLevel() => Mathf.Clamp(killCount, 1, MaxComboLevel);
    public float GetRemainingTime() => comboActive ? comboTimer : 0f;
    public bool IsComboActive() => comboActive;
}