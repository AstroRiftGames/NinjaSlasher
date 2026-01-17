using System.Collections;
using UnityEngine;

public class LevelTimerService
{
    private LevelSession session;
    private MonoBehaviour coroutineRunner;

    private float currentTime;
    private float initialDuration;
    private bool isPaused;
    private bool isRunning;

    private Coroutine timerCoroutine;

    public float CurrentTime => currentTime;
    public float InitialDuration => initialDuration;
    public float TimeRemaining => Mathf.Max(0, currentTime);
    public float TimeTaken => initialDuration - currentTime;
    public bool IsPaused => isPaused;

    public LevelTimerService(LevelSession levelSession, MonoBehaviour runner)
    {
        session = levelSession;
        coroutineRunner = runner;
        isPaused = false;
        isRunning = false;

        Debug.Log($"[LevelTimerService] Servicio creado");
    }

    public void Initialize()
    {
        float baseDuration = session.Configuration.levelDuration;
        float modifiedDuration = ApplyPowerUpModifiers(baseDuration);

        currentTime = modifiedDuration;
        initialDuration = baseDuration;
        isPaused = false;
        isRunning = false;

        Debug.Log($"[LevelTimerService] Inicializado - Tiempo: {currentTime:F1}s (base: {baseDuration:F1}s)");
    }

    public void Start()
    {
        if (isRunning)
        {
            Debug.LogWarning($"[LevelTimerService] Timer ya está corriendo");
            return;
        }

        isRunning = true;
        timerCoroutine = coroutineRunner.StartCoroutine(TimerCoroutine());

        Debug.Log($"[LevelTimerService] Timer iniciado");
    }

    public void Pause()
    {
        if (!isRunning)
        {
            Debug.LogWarning($"[LevelTimerService] Timer no está corriendo");
            return;
        }

        isPaused = true;
        Debug.Log($"[LevelTimerService] Timer pausado");
    }

    public void Resume()
    {
        if (!isPaused)
        {
            Debug.LogWarning($"[LevelTimerService] Timer no está pausado");
            return;
        }

        isPaused = false;
        Debug.Log($"[LevelTimerService] Timer resumido");
    }

    public void Stop()
    {
        if (timerCoroutine != null && coroutineRunner != null)
        {
            coroutineRunner.StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        isRunning = false;
        isPaused = false;

        Debug.Log($"[LevelTimerService] Timer detenido - Tiempo final: {TimeTaken:F2}s");
    }

    public void AddTime(float seconds)
    {
        if (!isRunning)
        {
            Debug.LogWarning($"[LevelTimerService] No se puede agregar tiempo - Timer no está corriendo");
            return;
        }

        currentTime += seconds;
        GameEvents.RaiseLevelTimeChanged(currentTime);

        Debug.Log($"[LevelTimerService] +{seconds:F1}s agregados - Tiempo actual: {currentTime:F1}s");
    }

    private IEnumerator TimerCoroutine()
    {
        while (currentTime > 0 && isRunning)
        {
            if (!isPaused)
            {
                currentTime -= Time.deltaTime;
                session.UpdateTime(TimeTaken);

                GameEvents.RaiseLevelTimeChanged(currentTime);

                if (currentTime <= 0)
                {
                    OnTimeExpired();
                    break;
                }
            }

            yield return null;
        }
    }

    private void OnTimeExpired()
    {
        currentTime = 0;
        isRunning = false;

        GameEvents.RaiseLevelTimeExpired();

        Debug.Log($"[LevelTimerService] ¡Tiempo agotado!");
    }

    private float ApplyPowerUpModifiers(float baseDuration)
    {
        float modifiedDuration = baseDuration;

        var powerUpContext = PowerUpManager.Instance?.context;
        if (powerUpContext != null && powerUpContext.ExtraTimeActive)
        {
            float extraPercent = powerUpContext.ExtraTimePercent;
            float bonusTime = baseDuration * extraPercent;
            modifiedDuration += bonusTime;

            Debug.Log($"[LevelTimerService] Power-up ExtraTime activo: +{bonusTime:F1}s ({extraPercent * 100:F0}%)");
        }

        return modifiedDuration;
    }

    public void Dispose()
    {
        Stop();
        Debug.Log($"[LevelTimerService] Servicio destruido");
    }
}