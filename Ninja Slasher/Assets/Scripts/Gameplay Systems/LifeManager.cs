using System;
using UnityEngine;

/// <summary>
/// Sistema de vidas con deducción virtual consistente:
/// - Si regenera/suma vidas durante un nivel, la deducción virtual se recalcula (CurrentLives - 1).
/// - Unifica el evento para UI: siempre emitimos las vidas "mostrables" (GetDisplayLives()).
/// - Carga en Awake() para evitar carreras con GameManager.Start().
/// - No duplica confirmación en pérdida de foco (dejamos que GameManager lo haga).
/// - Usa DateTime.UtcNow para mayor estabilidad temporal.
/// </summary>
public class LifeManager : MonoBehaviourSingleton<LifeManager>
{
    [Header("LIVES SETTINGS")]
    [SerializeField] private int _maxLives = 5;
    [SerializeField] private int _startingLives = 5;
    [SerializeField] private int _lifeRechargeSeconds = 1800;

    public int CurrentLives { get; private set; }
    private DateTime _lastLifeUsedUtc;

    [Header("VIRTUAL LIFE DEDUCTION")]
    private int _virtualLives;
    private bool _hasVirtualDeduction = false;

    private bool _levelInProgress = false;

    public event Action<int> OnLivesChanged;

    public override void Awake()
    {
        base.Awake();
        InitializeFromSave();
    }

    private void Start()
    {
        OnLivesChanged?.Invoke(GetDisplayLives());
    }

    private void Update()
    {
        UpdateLifeRecharge();
    }

    private void InitializeFromSave()
    {
        var data = SaveManager.Instance?.GetGameData();

        DateTime lastRegenUtc = DateTime.UtcNow;
        bool validDate = data != null && DateTime.TryParse(data.lastLifeRegenTime, out lastRegenUtc);

        bool isCorruptOrFirstTime =
            data == null ||
            data.currentLives < 0 ||
            data.currentLives > _maxLives ||
            !validDate;

        if (isCorruptOrFirstTime)
        {
            CurrentLives = Mathf.Clamp(_startingLives, 0, _maxLives);
            _lastLifeUsedUtc = DateTime.UtcNow;
            Persist("Init (default)");
        }
        else
        {
            CurrentLives = Mathf.Clamp(data.currentLives, 0, _maxLives);
            _lastLifeUsedUtc = DateTime.SpecifyKind(lastRegenUtc, DateTimeKind.Utc);
        }

        CheckOfflineRegeneration();
        _virtualLives = CurrentLives;
    }

    private void Persist(string reason = "Autosave")
    {
        bool hasTimer = CurrentLives < _maxLives;
        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.OnLivesChanged(CurrentLives, _lastLifeUsedUtc, hasTimer);
        }
        else
        {
            SaveManager.Instance?.UpdateLives(CurrentLives, _lastLifeUsedUtc, hasTimer);
        }
#if UNITY_EDITOR
        Debug.Log($"[LifeManager] Persist -> {reason}. Lives={CurrentLives}, lastUsedUtc={_lastLifeUsedUtc:O}, timer={(hasTimer ? "ON" : "OFF")}");
#endif
    }

    private void UpdateLifeRecharge()
    {
        if (CurrentLives >= _maxLives) return;

        double seconds = (DateTime.UtcNow - _lastLifeUsedUtc).TotalSeconds;
        if (seconds < _lifeRechargeSeconds) return;

        int toGenerate = Mathf.FloorToInt((float)seconds / _lifeRechargeSeconds);
        int newLives = Mathf.Min(CurrentLives + toGenerate, _maxLives);

        _lastLifeUsedUtc = _lastLifeUsedUtc.AddSeconds(toGenerate * _lifeRechargeSeconds);
        CurrentLives = newLives;

        if (_hasVirtualDeduction)
            _virtualLives = Mathf.Max(0, CurrentLives - 1);
        else
            _virtualLives = CurrentLives;

        Persist("Vida regenerada");
        EmitDisplayLivesChanged();
    }

    private void CheckOfflineRegeneration()
    {
        if (CurrentLives >= _maxLives) return;

        double seconds = (DateTime.UtcNow - _lastLifeUsedUtc).TotalSeconds;
        if (seconds < _lifeRechargeSeconds) return;

        int toGenerate = Mathf.FloorToInt((float)seconds / _lifeRechargeSeconds);
        int newLives = Mathf.Min(CurrentLives + toGenerate, _maxLives);

        _lastLifeUsedUtc = _lastLifeUsedUtc.AddSeconds(toGenerate * _lifeRechargeSeconds);
        CurrentLives = newLives;

        _virtualLives = _hasVirtualDeduction ? Mathf.Max(0, CurrentLives - 1) : CurrentLives;

        Persist("Vidas offline regeneradas");
        EmitDisplayLivesChanged();
    }

    public bool CanPlay() => CurrentLives > 0;

    public void OnLevelStart()
    {
        _hasVirtualDeduction = false;

        if (CurrentLives > 0)
        {
            _virtualLives = Mathf.Max(0, CurrentLives - 1);
            _hasVirtualDeduction = true;
            _levelInProgress = true;

            EmitDisplayLivesChanged();
        }
    }

    public void UseLife()
    {
        var context = PowerUpManager.Instance?.context;
        if (context != null && context.SecondChanceActive)
        {
            Debug.Log("[PowerUp] Second Chance: vida NO restada.");
            EmitDisplayLivesChanged();
            return;
        }

        if (_hasVirtualDeduction)
        {
            CurrentLives = Mathf.Clamp(_virtualLives, 0, _maxLives);
            _lastLifeUsedUtc = DateTime.UtcNow;

            _hasVirtualDeduction = false;
            _levelInProgress = false;

            Persist("Vida perdida (confirmada)");
            EmitDisplayLivesChanged();

            Debug.Log($"[LifeManager] Nivel perdido. Descuento confirmado. Vidas: {CurrentLives}");
        }
        else
        {
            if (CurrentLives <= 0) return;

            CurrentLives = Mathf.Max(0, CurrentLives - 1);
            _lastLifeUsedUtc = DateTime.UtcNow;
            _virtualLives = CurrentLives;

            Persist("Vida perdida (directa)");
            EmitDisplayLivesChanged();

            Debug.Log($"[LifeManager] Vida usada. Restantes: {CurrentLives}");
        }
    }

    public void OnLevelCompleted()
    {
        if (_hasVirtualDeduction)
        {
            _virtualLives = CurrentLives;
            _hasVirtualDeduction = false;
            _levelInProgress = false;

            Debug.Log($"[LifeManager] Nivel completado. Descuento cancelado. Vidas mantenidas: {CurrentLives}");
            EmitDisplayLivesChanged();
        }
    }

    public void OnLevelExit()
    {
        if (_hasVirtualDeduction)
        {
            CurrentLives = Mathf.Clamp(_virtualLives, 0, _maxLives);
            _lastLifeUsedUtc = DateTime.UtcNow;

            _hasVirtualDeduction = false;
            _levelInProgress = false;

            Persist("Vida perdida por abandono");
            EmitDisplayLivesChanged();

            Debug.Log($"[LifeManager] Nivel abandonado. Descuento confirmado. Vidas: {CurrentLives}");
        }
    }

    public int GetDisplayLives() => _hasVirtualDeduction ? _virtualLives : CurrentLives;
    public int GetRealLives() => CurrentLives;

    public void AddLife()
    {
        if (CurrentLives >= _maxLives) return;

        CurrentLives++;

        if (_hasVirtualDeduction)
            _virtualLives = Mathf.Max(0, CurrentLives - 1);
        else
            _virtualLives = CurrentLives;

        Persist("Vida ganada");
        EmitDisplayLivesChanged();

        Debug.Log($"[LifeManager] Vida agregada. Total: {CurrentLives}");
    }

    public void FillAllLives()
    {
        if (CurrentLives >= _maxLives) return;

        int previousLives = CurrentLives;
        CurrentLives = _maxLives;
        _lastLifeUsedUtc = DateTime.UtcNow;

        if (_hasVirtualDeduction)
            _virtualLives = Mathf.Max(0, CurrentLives - 1);
        else
            _virtualLives = CurrentLives;

        Persist("Vidas completas");
        EmitDisplayLivesChanged();

        Debug.Log($"[LifeManager] Vidas llenadas: {previousLives} -> {CurrentLives}");
    }

    public float GetRechargeProgress()
    {
        if (CurrentLives >= _maxLives) return 1f;
        double seconds = (DateTime.UtcNow - _lastLifeUsedUtc).TotalSeconds;
        return Mathf.Clamp01((float)(seconds / _lifeRechargeSeconds));
    }

    public TimeSpan GetTimeToNextLife()
    {
        if (CurrentLives >= _maxLives) return TimeSpan.Zero;
        double seconds = (DateTime.UtcNow - _lastLifeUsedUtc).TotalSeconds;
        double secondsLeft = _lifeRechargeSeconds - seconds;
        return TimeSpan.FromSeconds(Mathf.Max(0, (float)secondsLeft));
    }

    public bool HasPendingDeduction() => _hasVirtualDeduction;

    private void OnApplicationFocus(bool hasFocus) { }

    private void EmitDisplayLivesChanged()
    {
        OnLivesChanged?.Invoke(GetDisplayLives());
    }
}
