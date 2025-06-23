using System;
using UnityEngine;

public class LifeManager : MonoBehaviourSingleton<LifeManager>
{
    [SerializeField] private int _maxLives;
    [SerializeField] private int _startingLives;
    [SerializeField] private int _lifeRechargeSeconds;

    public int CurrentLives { get; private set; }
    private DateTime _lastLifeUsed;

    public event Action<int> OnLivesChanged;
    private bool _isRecharging = false;

    public override void Awake()
    {
        base.Awake();
        CurrentLives = Mathf.Clamp(_startingLives, 1, _maxLives);
        _lastLifeUsed = DateTime.Now;
        Debug.Log("[LifeManager] Vidas al iniciar: " + CurrentLives);
    }

    private void Update()
    {
        if (_isRecharging)
            UpdateLifeRecharge();
    }

    public bool CanPlay()
    {
        return CurrentLives > 0;
    }

    public void UseLife()
    {
        if (CurrentLives <= 0)
            return;

        CurrentLives--;

        if (!_isRecharging)
        {
            _isRecharging = true;
            _lastLifeUsed = DateTime.Now;
        }

        Debug.Log($"[LifeManager] Usaste una vida. Quedan: {CurrentLives}");
        OnLivesChanged?.Invoke(CurrentLives);
    }


    public void AddLife()
    {
        if (CurrentLives < _maxLives)
        {
            CurrentLives++;
            Debug.Log("[LifeManager] Sumaste una vida. Quedan: " + CurrentLives);
            OnLivesChanged?.Invoke(CurrentLives);
        }
    }

    private void UpdateLifeRecharge()
    {
        if (CurrentLives >= _maxLives)
        {
            _isRecharging = false;
            return;
        }

        double secondsSince = (DateTime.Now - _lastLifeUsed).TotalSeconds;
        double secondsRemaining = _lifeRechargeSeconds - secondsSince;

        TimeSpan remaining = TimeSpan.FromSeconds(Mathf.Max(0, (float)secondsRemaining));
        Debug.Log($"[LifeManager] Tiempo para próxima vida: {remaining.Minutes:D2}:{remaining.Seconds:D2}");

        if (secondsSince >= _lifeRechargeSeconds)
        {
            CurrentLives = Mathf.Min(CurrentLives + 1, _maxLives);
            _lastLifeUsed = DateTime.Now;
            Debug.Log($"[LifeManager] +1 vida recargada. Total: {CurrentLives}");
            OnLivesChanged?.Invoke(CurrentLives);
        }
    }

    public float GetRechargeProgress()
    {
        if (CurrentLives >= _maxLives) return 1f;

        double secondsSince = (DateTime.Now - _lastLifeUsed).TotalSeconds;
        return Mathf.Clamp01((float)(secondsSince / _lifeRechargeSeconds));
    }

    public TimeSpan GetTimeToNextLife()
    {
        if (CurrentLives >= _maxLives) return TimeSpan.Zero;

        double secondsSince = (DateTime.Now - _lastLifeUsed).TotalSeconds;
        double secondsLeft = _lifeRechargeSeconds - secondsSince;
        return TimeSpan.FromSeconds(Mathf.Max(0, (float)secondsLeft));
    }
}
