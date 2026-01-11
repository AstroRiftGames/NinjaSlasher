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

    private int killCount = 0;
    private float comboTimer = 0f;
    private bool comboActive = false;
    private Vector3 lastEnemyPosition;

    public override void Awake()
    {
        base.Awake();
    }

    void Update()
    {
        if (!comboActive) return;

        comboTimer -= Time.deltaTime;

        if (comboTimer <= 0f)
        {
            ResetCombo();
        }
    }

    public void RegisterKill(Vector3 enemyPosition)
    {
        lastEnemyPosition = enemyPosition;
        killCount++;
        int level = Mathf.Clamp(killCount, 1, 5);

        comboTimer = level switch
        {
            1 => 2f,
            2 => 1.6f,
            3 => 1.4f,
            4 => 1.2f,
            _ => 1f
        };

        comboActive = true;

        if (level >= 2)
        {
            GiveBonus(level);

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnComboPerformed();
            }

            Debug.Log($"[ComboManager] Combo x{level} activado en posición {lastEnemyPosition}");

            GameEvents.RaiseComboUpdated(level, lastEnemyPosition);

            // DEPRECATED
            //OnComboUpdated?.Invoke(level);
            //OnComboUpdatedWithPosition?.Invoke(level, lastEnemyPosition);
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

            Debug.Log($"[ComboManager] Bonus aumentado por ComboMaster: {bonus:F1}s (base + {bonusExtra:F1}s)");
        }

        GameEvents.RaiseLevelTimeBonus(bonus);

        Debug.Log($"[ComboManager] Bonus de tiempo otorgado: +{bonus:F1}s");
    }

    private void ResetCombo()
    {
        if (killCount > 1)
        {
            Debug.Log($"[ComboManager] Combo x{killCount} terminado");
        }

        killCount = 0;
        comboActive = false;

        GameEvents.RaiseComboReset();

        // DEPRECATED
        //OnComboEnded?.Invoke();
    }

    public Vector3 GetLastEnemyPosition()
    {
        return lastEnemyPosition;
    }

    public int GetCurrentComboLevel() => Mathf.Clamp(killCount, 1, 5);
    public float GetRemainingTime() => comboActive ? comboTimer : 0f;
    public bool IsComboActive() => comboActive;
}