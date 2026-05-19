using System;
using System.Collections;
using UnityEngine;

public sealed class StarRevealCelebrationEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] _particleSystems;
    [SerializeField] private Color[] _fireworkColors =
    {
        new Color(1f, 0.42f, 0.38f, 1f),     // red
        new Color(0.46f, 1f, 0.6f, 1f),      // green
        new Color(0.48f, 0.72f, 1f, 1f),     // blue
        new Color(1f, 0.96f, 0.58f, 1f),     // yellow
        new Color(0.86f, 0.58f, 1f, 1f),     // purple
        new Color(1f, 0.74f, 0.42f, 1f),     // orange
        new Color(1f, 0.99f, 0.98f, 1f),     // white / silver
        new Color(1f, 0.88f, 0.54f, 1f)      // gold
    };
    [SerializeField, Min(0f)] private float _leadDelayBeforeStarReveal = 0.12f;
    [SerializeField, Min(0f)] private float _releaseBufferAfterPlayback = 0.05f;

    private Action<StarRevealCelebrationEffect> _releaseAction;
    private Coroutine _releaseRoutine;
    private ParticleSystem.Particle[] _particleBuffer;

    public float LeadDelayBeforeStarReveal => _leadDelayBeforeStarReveal;
    public float TotalDuration => CalculateMaxPlaybackDuration() + _releaseBufferAfterPlayback;

    public void Initialize(Action<StarRevealCelebrationEffect> releaseAction)
    {
        _releaseAction = releaseAction;
    }

    public void Play()
    {
        if (!HasConfiguredParticleSystems(logWarning: true))
        {
            if (Application.isPlaying)
                ReleaseToPool();
            return;
        }

        gameObject.SetActive(true);

        if (Application.isPlaying && !isActiveAndEnabled)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("[StarRevealCelebrationEffect] Play was requested while the effect is inactive in hierarchy.", this);
#endif
            ReleaseToPool();
            return;
        }

        float playbackDuration = 0f;
        for (int i = 0; i < _particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = _particleSystems[i];
            if (particleSystem == null)
                continue;

            particleSystem.Clear(true);
            particleSystem.Play(true);
            if (Application.isPlaying)
                StartCoroutine(ApplyFireworkColorsAfterEmission(particleSystem));
            playbackDuration = Mathf.Max(playbackDuration, GetPlaybackDuration(particleSystem));
        }

        if (!Application.isPlaying)
            return;

        RestartReleaseRoutine(playbackDuration + _releaseBufferAfterPlayback);
    }

    public void Stop()
    {
        StopReleaseRoutine();

        if (!HasConfiguredParticleSystems(logWarning: false))
            return;

        for (int i = 0; i < _particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = _particleSystems[i];
            if (particleSystem == null)
                continue;

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(true);
        }
    }

    public void ResetEffect()
    {
        Stop();
    }

    public void StopAndRelease()
    {
        Stop();
        ReleaseToPool();
    }

    private void RestartReleaseRoutine(float delay)
    {
        StopReleaseRoutine();
        _releaseRoutine = StartCoroutine(ReleaseAfterPlayback(delay));
    }

    private void StopReleaseRoutine()
    {
        if (_releaseRoutine == null)
            return;

        StopCoroutine(_releaseRoutine);
        _releaseRoutine = null;
    }

    private IEnumerator ReleaseAfterPlayback(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        _releaseRoutine = null;
        ReleaseToPool();
    }

    private void ReleaseToPool()
    {
        transform.SetParent(null, false);
        gameObject.SetActive(false);
        _releaseAction?.Invoke(this);
    }

    private float CalculateMaxPlaybackDuration()
    {
        if (!HasConfiguredParticleSystems(logWarning: false))
            return 0f;

        float maxDuration = 0f;
        for (int i = 0; i < _particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = _particleSystems[i];
            if (particleSystem == null)
                continue;

            maxDuration = Mathf.Max(maxDuration, GetPlaybackDuration(particleSystem));
        }

        return maxDuration;
    }

    private void ApplyFireworkColors(ParticleSystem particleSystem)
    {
        if (_fireworkColors == null || _fireworkColors.Length == 0)
            return;

        int particleCount = particleSystem.particleCount;
        if (particleCount <= 0)
            return;

        EnsureParticleBufferCapacity(particleCount);
        particleCount = particleSystem.GetParticles(_particleBuffer);

        for (int i = 0; i < particleCount; i++)
        {
            Color color = _fireworkColors[UnityEngine.Random.Range(0, _fireworkColors.Length)];
            _particleBuffer[i].startColor = color;
        }

        particleSystem.SetParticles(_particleBuffer, particleCount);
    }

    private IEnumerator ApplyFireworkColorsAfterEmission(ParticleSystem particleSystem)
    {
        yield return null;

        if (particleSystem == null || !particleSystem.isPlaying)
            yield break;

        ApplyFireworkColors(particleSystem);
    }

    private void EnsureParticleBufferCapacity(int particleCount)
    {
        if (_particleBuffer != null && _particleBuffer.Length >= particleCount)
            return;

        _particleBuffer = new ParticleSystem.Particle[particleCount];
    }

    private bool HasConfiguredParticleSystems(bool logWarning)
    {
        if (_particleSystems == null || _particleSystems.Length == 0)
        {
            if (logWarning)
                LogMissingReferenceWarning("Particle systems array is not configured.");
            return false;
        }

        bool hasValidParticleSystem = false;
        for (int i = 0; i < _particleSystems.Length; i++)
        {
            if (_particleSystems[i] != null)
            {
                hasValidParticleSystem = true;
                continue;
            }

            if (logWarning)
                LogMissingReferenceWarning($"Particle system reference at index {i} is null.");
        }

        if (!hasValidParticleSystem && logWarning)
            LogMissingReferenceWarning("No valid particle systems were found.");

        return hasValidParticleSystem;
    }

    private void LogMissingReferenceWarning(string message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning($"[StarRevealCelebrationEffect] {message}", this);
#endif
    }

    private static float GetPlaybackDuration(ParticleSystem particleSystem)
    {
        ParticleSystem.MainModule main = particleSystem.main;
        return GetMaxCurveValue(main.startDelay) + main.duration + GetMaxCurveValue(main.startLifetime);
    }

    private static float GetMaxCurveValue(ParticleSystem.MinMaxCurve curve)
    {
        return curve.mode switch
        {
            ParticleSystemCurveMode.Constant => Mathf.Max(0f, curve.constant),
            ParticleSystemCurveMode.TwoConstants => Mathf.Max(0f, Mathf.Max(curve.constantMin, curve.constantMax)),
            ParticleSystemCurveMode.Curve => Mathf.Max(0f, GetMaxAnimationCurveValue(curve.curve) * curve.curveMultiplier),
            ParticleSystemCurveMode.TwoCurves => Mathf.Max(
                0f,
                Mathf.Max(GetMaxAnimationCurveValue(curve.curveMin), GetMaxAnimationCurveValue(curve.curveMax)) * curve.curveMultiplier),
            _ => 0f
        };
    }

    private static float GetMaxAnimationCurveValue(AnimationCurve curve)
    {
        if (curve == null || curve.length == 0)
            return 0f;

        float maxValue = curve.keys[0].value;
        for (int i = 1; i < curve.length; i++)
            maxValue = Mathf.Max(maxValue, curve.keys[i].value);

        return maxValue;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Play Test")]
    private void ContextMenuPlayTest()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        Play();
    }

    [ContextMenu("Debug/Stop Test")]
    private void ContextMenuStopTest()
    {
        Stop();
    }

    private void Reset()
    {
        CacheParticleSystems();
    }

    private void OnValidate()
    {
        CacheParticleSystems();
        _leadDelayBeforeStarReveal = Mathf.Max(0f, _leadDelayBeforeStarReveal);
        _releaseBufferAfterPlayback = Mathf.Max(0f, _releaseBufferAfterPlayback);
    }

    private void CacheParticleSystems()
    {
        if (_particleSystems == null || _particleSystems.Length == 0)
            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }
#endif

    private void OnDisable()
    {
        _releaseRoutine = null;
    }
}
