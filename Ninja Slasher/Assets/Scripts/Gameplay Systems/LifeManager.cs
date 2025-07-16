using System;
using UnityEngine;

public class LifeManager : MonoBehaviourSingleton<LifeManager>
{
    [Header("Ajustes de vidas")]
    [SerializeField] private int _maxLives;
    [SerializeField] private int _startingLives;
    [SerializeField] private int _lifeRechargeSeconds = 1800; // 30 min por vida

    public int CurrentLives { get; private set; }
    private DateTime _lastLifeUsed;

    public event Action<int> OnLivesChanged;
    private bool _secondChanceUsed = false;

    private void Start()
    {
        LoadLivesFromSave();
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

            SaveLivesViaAutoSave("Vida regenerada");
            OnLivesChanged?.Invoke(CurrentLives);

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

            SaveLivesViaAutoSave("Vidas offline regeneradas");
            OnLivesChanged?.Invoke(CurrentLives);

            Debug.Log($"[LifeManager] Regeneración offline: {vidasAGenerar} vidas. Total: {CurrentLives}");
        }
    }

    public bool CanPlay() => CurrentLives > 0;

    public void UseLife()
    {
        var context = PowerUpManager.Instance?.context;
        if (context != null && context.SecondChanceActive)
        {
            Debug.Log("[PowerUp] Second Chance: vida NO restada.");
            return;
        }

        if (CurrentLives <= 0) return;

        CurrentLives--;
        _lastLifeUsed = DateTime.Now;

        SaveLivesViaAutoSave("Vida perdida");
        OnLivesChanged?.Invoke(CurrentLives);

        Debug.Log($"[LifeManager] Vida usada. Restantes: {CurrentLives}");
    }

    public void AddLife()
    {
        if (CurrentLives >= _maxLives) return;

        CurrentLives++;

        SaveLivesViaAutoSave("Vida ganada");
        OnLivesChanged?.Invoke(CurrentLives);

        Debug.Log($"[LifeManager] Vida agregada. Total: {CurrentLives}");
    }

    public void FillAllLives()
    {
        if (CurrentLives >= _maxLives) return;

        int previousLives = CurrentLives;
        CurrentLives = _maxLives;
        _lastLifeUsed = DateTime.Now;

        SaveLivesViaAutoSave("Vidas completas");
        OnLivesChanged?.Invoke(CurrentLives);

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

    private void SaveLivesViaAutoSave(string message = "Guardando vidas...")
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

#if UNITY_EDITOR
    [ContextMenu("Usar Vida")]
    public void DebugUseLife() => UseLife();

    [ContextMenu("Agregar Vida")]
    public void DebugAddLife() => AddLife();

    [ContextMenu("Mostrar Estado")]
    public void DebugShowState()
    {
        Debug.Log($"[LIFE MANAGER] \nVidas: {CurrentLives}\nUltima vida usada: {_lastLifeUsed}\nTiempo hasta la próxima vida: {GetTimeToNextLife()}");
    }
#endif
}
