using UnityEngine;
using System;

public class ComboManager : MonoBehaviourSingleton<ComboManager>
{
    public event Action<int> OnComboUpdated;
    public event Action OnComboEnded;

    private int killCount = 0;
    private float comboTimer = 0f;
    private bool comboActive = false;
    private LevelController levelController;

    public override void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        FindLevelController();
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

    private void FindLevelController()
    {
        levelController = FindObjectOfType<LevelController>();

        if (levelController == null)
        {
            Debug.LogError("[ComboManager] No se pudo encontrar LevelController en la escena!");
        }
        else
        {
            Debug.Log($"[ComboManager] LevelController encontrado: {levelController.name}");
        }
    }

    public void RegisterKill()
    {
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
            GiveBonus(level);

        OnComboUpdated?.Invoke(level);
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
            Debug.Log($"[PowerUp] ComboMaster: +{bonusExtra:F2}s extra en combo ({percent * 100}% adicional).");
        }

        if (levelController == null)
        {
            Debug.Log("[ComboManager] LevelController es null, intentando buscar de nuevo...");
            FindLevelController();
        }

        if (levelController != null)
        {
            Debug.Log($"[ComboManager] Agregando {bonus:F2} segundos al timer");
            levelController.AddTime(bonus);
        }
        else
        {
            Debug.LogError("[ComboManager] LevelController sigue siendo NULL! Verificar que esté en la escena.");
        }
    }

    private void ResetCombo()
    {
        killCount = 0;
        comboActive = false;
        OnComboEnded?.Invoke();
    }
}
