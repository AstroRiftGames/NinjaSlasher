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
    private float elapsedTime;

    private Coroutine timerCoroutine;

    public float CurrentTime => currentTime;
    public float InitialDuration => initialDuration;
    public float TimeRemaining => Mathf.Max(0, currentTime);
    public float TimeTaken => elapsedTime;
    public bool IsPaused => isPaused;

    public LevelTimerService(LevelSession levelSession, MonoBehaviour runner)
    {
        session = levelSession;
        coroutineRunner = runner;
        isPaused = false;
        isRunning = false;
        elapsedTime = 0f;
    }

    public void Initialize()
    {
        float baseDuration = session.Configuration.levelDuration;
        float modifiedDuration = ApplyPowerUpModifiers(baseDuration);

        currentTime = modifiedDuration;
        initialDuration = baseDuration;
        isPaused = false;
        isRunning = false;
        elapsedTime = 0f;
    }

    public void Start()
    {
        if (isRunning)
        {
            return;
        }

        isRunning = true;
        elapsedTime = 0f;
        timerCoroutine = coroutineRunner.StartCoroutine(TimerCoroutine());
    }

    public void Pause()
    {
        if (!isRunning)
        {
            return;
        }

        isPaused = true;
    }

    public void Resume()
    {
        if (!isPaused)
        {
            return;
        }

        isPaused = false;
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
    }

    public void AddTime(float seconds)
    {
        if (!isRunning)
        {
            return;
        }

        currentTime += seconds;
        GameEvents.RaiseLevelTimeChanged(currentTime);
    }

    private IEnumerator TimerCoroutine()
    {
        while (currentTime > 0 && isRunning)
        {
            if (!isPaused)
            {
                float deltaTime = Time.deltaTime;
                currentTime -= deltaTime;
                elapsedTime += deltaTime;

                session.UpdateTime(elapsedTime);

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
        }

        return modifiedDuration;
    }

    public void Dispose()
    {
        Stop();
    }
}