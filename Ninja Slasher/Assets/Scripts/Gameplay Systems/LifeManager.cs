using System;
using UnityEngine;

public class LifeManager : MonoBehaviourSingleton<LifeManager>
{
    [Header("LIVES SETTINGS")]
    [SerializeField] private int _maxLives;
    [SerializeField] private int _startingLives;
    [SerializeField] private int _lifeRechargeSeconds = 1800; // 30 min por vida

    public int CurrentLives { get; private set; }
    private DateTime _lastLifeUsed;

    [Header("VIRTUAL LIFE DISCOUNTING SYSTEM")]
    private int _virtualLives;
    private bool _hasVirtualDeduction = false;
    private bool _levelInProgress = false;

    public event Action<int> OnLivesChanged;
    private bool _secondChanceUsed = false;

    private void Start()
    {
        LoadLivesFromSave();
        _virtualLives = CurrentLives;
    }

    private void LoadLivesFromSave()
    {
        var data = SaveManager.Instance.GetGameData();

        DateTime lastRegenTime = DateTime.Now;
        bool validDate = data != null && DateTime.TryParse(data.lastLifeRegenTime, out lastRegenTime);

        bool isSaveCorruptOrFirstTime =
            data == null ||
            data.currentLives < 0 ||
            data.currentLives > _maxLives ||
            !validDate;

        if (isSaveCorruptOrFirstTime)
        {
            CurrentLives = _startingLives;
            _lastLifeUsed = DateTime.Now;

            SaveLivesViaAutoSave();
        }
        else
        {
            CurrentLives = data.currentLives;
            _lastLifeUsed = lastRegenTime;
        }

        CheckOfflineRegeneration();
        OnLivesChanged?.Invoke(CurrentLives);
    }

    private void Update()
    {
        UpdateLifeRecharge();
    }

    private void UpdateLifeRecharge()
    {
        if (CurrentLives >= _maxLives) return;

        double seconds = (DateTime.Now - _lastLifeUsed).TotalSeconds;
        if (seconds >= _lifeRechargeSeconds)
        {
            int vidasAGenerar = Mathf.FloorToInt((float)seconds / _lifeRechargeSeconds);
            int newLives = Mathf.Min(CurrentLives + vidasAGenerar, _maxLives);

            _lastLifeUsed = _lastLifeUsed.AddSeconds(vidasAGenerar * _lifeRechargeSeconds);
            CurrentLives = newLives;

            if (!_hasVirtualDeduction)
            {
                _virtualLives = CurrentLives;
            }

            SaveLivesViaAutoSave("Vida regenerada");

            if (!_hasVirtualDeduction)
            {
                OnLivesChanged?.Invoke(CurrentLives);
            }

            Debug.Log($"[LifeManager] Regeneradas {vidasAGenerar} vidas. Total: {CurrentLives}");
        }
    }

    private void CheckOfflineRegeneration()
    {
        if (CurrentLives >= _maxLives) return;

        double seconds = (DateTime.Now - _lastLifeUsed).TotalSeconds;
        if (seconds >= _lifeRechargeSeconds)
        {
            int vidasAGenerar = Mathf.FloorToInt((float)seconds / _lifeRechargeSeconds);
            int newLives = Mathf.Min(CurrentLives + vidasAGenerar, _maxLives);

            _lastLifeUsed = _lastLifeUsed.AddSeconds(vidasAGenerar * _lifeRechargeSeconds);
            CurrentLives = newLives;
            _virtualLives = CurrentLives;

            SaveLivesViaAutoSave("Vidas offline regeneradas");
            OnLivesChanged?.Invoke(CurrentLives);

            Debug.Log($"[LifeManager] Regeneración offline: {vidasAGenerar} vidas. Total: {CurrentLives}");
        }
    }

    public bool CanPlay() => CurrentLives > 0;

    public void OnLevelStart()
    {
        if (CurrentLives > 0 && !_hasVirtualDeduction)
        {
            _virtualLives = CurrentLives - 1;
            _hasVirtualDeduction = true;
            _levelInProgress = true;

            Debug.Log($"[LifeManager] Nivel iniciado. Vidas reales: {CurrentLives}, Vidas virtuales: {_virtualLives}");

            OnLivesChanged?.Invoke(_virtualLives);
        }
    }

    public void UseLife()
    {
        var context = PowerUpManager.Instance?.context;
        if (context != null && context.SecondChanceActive)
        {
            Debug.Log("[PowerUp] Second Chance: vida NO restada.");
            return;
        }

        if (_hasVirtualDeduction)
        {
            CurrentLives = _virtualLives;
            _lastLifeUsed = DateTime.Now;
            _hasVirtualDeduction = false;
            _levelInProgress = false;

            Debug.Log($"[LifeManager] Nivel perdido. Descuento confirmado. Vidas: {CurrentLives}");

            SaveLivesViaAutoSave("Vida perdida");
        }
        else
        {
            if (CurrentLives <= 0) return;

            CurrentLives--;
            _lastLifeUsed = DateTime.Now;
            _virtualLives = CurrentLives;

            SaveLivesViaAutoSave("Vida perdida");
            OnLivesChanged?.Invoke(CurrentLives);

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

            OnLivesChanged?.Invoke(CurrentLives);
        }
    }

    public void OnLevelExit()
    {
        if (_hasVirtualDeduction)
        {
            CurrentLives = _virtualLives;
            _lastLifeUsed = DateTime.Now;
            _hasVirtualDeduction = false;
            _levelInProgress = false;

            Debug.Log($"[LifeManager] Nivel abandonado. Descuento confirmado. Vidas: {CurrentLives}");

            SaveLivesViaAutoSave("Vida perdida por abandono");
        }
    }

    public int GetDisplayLives()
    {
        return _hasVirtualDeduction ? _virtualLives : CurrentLives;
    }

    public int GetRealLives()
    {
        return CurrentLives;
    }

    public void AddLife()
    {
        if (CurrentLives >= _maxLives) return;

        CurrentLives++;

        if (!_hasVirtualDeduction)
        {
            _virtualLives = CurrentLives;
        }

        SaveLivesViaAutoSave("Vida ganada");

        if (!_hasVirtualDeduction)
        {
            OnLivesChanged?.Invoke(CurrentLives);
        }

        Debug.Log($"[LifeManager] Vida agregada. Total: {CurrentLives}");
    }

    public void FillAllLives()
    {
        if (CurrentLives >= _maxLives) return;

        int previousLives = CurrentLives;
        CurrentLives = _maxLives;
        _lastLifeUsed = DateTime.Now;

        if (!_hasVirtualDeduction)
        {
            _virtualLives = CurrentLives;
        }

        SaveLivesViaAutoSave("Vidas completas");

        if (!_hasVirtualDeduction)
        {
            OnLivesChanged?.Invoke(CurrentLives);
        }

        Debug.Log($"[LifeManager] Vidas llenadas: {previousLives} -> {CurrentLives}");
    }

    public float GetRechargeProgress()
    {
        if (CurrentLives >= _maxLives) return 1f;
        double seconds = (DateTime.Now - _lastLifeUsed).TotalSeconds;
        return Mathf.Clamp01((float)(seconds / _lifeRechargeSeconds));
    }

    public TimeSpan GetTimeToNextLife()
    {
        if (CurrentLives >= _maxLives) return TimeSpan.Zero;
        double seconds = (DateTime.Now - _lastLifeUsed).TotalSeconds;
        double secondsLeft = _lifeRechargeSeconds - seconds;
        return TimeSpan.FromSeconds(Mathf.Max(0, (float)secondsLeft));
    }

    private void SaveLivesViaAutoSave(string message = "Guardando...")
    {
        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.OnLivesChanged(CurrentLives, _lastLifeUsed, CurrentLives < _maxLives);
        }
        else
        {
            SaveManager.Instance.UpdateLives(CurrentLives, _lastLifeUsed, CurrentLives < _maxLives);
            Debug.LogWarning("[LifeManager] AutoSaveManager no encontrado, guardando directamente");
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && _levelInProgress && _hasVirtualDeduction)
        {
            OnLevelExit();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && _levelInProgress && _hasVirtualDeduction)
        {
            OnLevelExit();
        }
    }

    public bool HasPendingDeduction()
    {
        return _hasVirtualDeduction;
    }
}