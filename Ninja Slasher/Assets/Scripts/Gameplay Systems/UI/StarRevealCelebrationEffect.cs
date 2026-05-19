using System;
using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class StarRevealCelebrationEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] _particleSystems;
    [SerializeField, Min(0f)] private float _sfxDelay = 0.2f;
    [SerializeField, Min(0f)] private float _minimumSecondsBetweenSounds = 0.35f;
    [SerializeField, Min(0f)] private float _leadDelayBeforeStarReveal = 0.12f;
    [SerializeField, Min(0f)] private float _totalDuration = 0.9f;

    private Action<StarRevealCelebrationEffect> _releaseAction;
    private Coroutine _releaseRoutine;
    private Coroutine _soundRoutine;
    private float _lastSfxRequestTime = float.NegativeInfinity;
    private UIAudioContext _audioContext;
#if UNITY_EDITOR
    private bool _isEditorPreviewPlaying;
    private double _editorPreviewStartTime;
    private double _editorPreviewLastTime;
    private bool[] _editorPreviewOriginalAutoRandomSeeds;
    private uint[] _editorPreviewOriginalRandomSeeds;
#endif

    public float LeadDelayBeforeStarReveal => _leadDelayBeforeStarReveal;
    public float TotalDuration => _totalDuration;

    public void Initialize(Action<StarRevealCelebrationEffect> releaseAction)
    {
        _releaseAction = releaseAction;
        _audioContext ??= GetComponentInParent<UIAudioContext>();
    }

    public void Play()
    {
        gameObject.SetActive(true);
        StopReleaseRoutine();
        StopSoundRoutine();

        bool hasValidParticleSystem = false;
        for (int i = 0; i < _particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = _particleSystems[i];
            if (particleSystem == null)
                continue;

            hasValidParticleSystem = true;
            particleSystem.Clear(true);
            particleSystem.Play(true);
        }

        if (!Application.isPlaying)
            return;

        if (!hasValidParticleSystem)
        {
            ReleaseToPool();
            return;
        }

        TryPlayCelebrationSfx();
        _releaseRoutine = StartCoroutine(ReleaseAfterPlayback());
    }

    public void Stop()
    {
        StopReleaseRoutine();
        StopSoundRoutine();

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

    private void TryPlayCelebrationSfx()
    {
        AudioEvent celebrationSfx = GetCelebrationSfx();
        if (celebrationSfx == null)
            return;

        if (AudioService.Instance == null)
            return;

        float currentUnscaledTime = Time.unscaledTime;
        if (currentUnscaledTime - _lastSfxRequestTime < _minimumSecondsBetweenSounds)
            return;

        _lastSfxRequestTime = currentUnscaledTime;

        if (_sfxDelay <= 0f)
        {
            PlayCelebrationSfxNow();
            return;
        }

        _soundRoutine = StartCoroutine(PlayCelebrationSfxAfterDelay());
    }

    private IEnumerator PlayCelebrationSfxAfterDelay()
    {
        yield return new WaitForSecondsRealtime(_sfxDelay);
        _soundRoutine = null;
        PlayCelebrationSfxNow();
    }

    private void PlayCelebrationSfxNow()
    {
        AudioService audioService = AudioService.Instance;
        AudioEvent celebrationSfx = GetCelebrationSfx();
        if (audioService == null || celebrationSfx == null)
            return;

        audioService.PlaySFX(celebrationSfx);
    }

    private AudioEvent GetCelebrationSfx()
    {
        _audioContext ??= GetComponentInParent<UIAudioContext>();
        return _audioContext != null && _audioContext.Audio != null
            ? _audioContext.Audio.starRevealFireworks
            : null;
    }

    private IEnumerator ReleaseAfterPlayback()
    {
        if (_totalDuration > 0f)
            yield return new WaitForSecondsRealtime(_totalDuration);

        _releaseRoutine = null;
        ReleaseToPool();
    }

    private void StopReleaseRoutine()
    {
        if (_releaseRoutine == null)
            return;

        StopCoroutine(_releaseRoutine);
        _releaseRoutine = null;
    }

    private void StopSoundRoutine()
    {
        if (_soundRoutine == null)
            return;

        StopCoroutine(_soundRoutine);
        _soundRoutine = null;
    }

    private void ReleaseToPool()
    {
        transform.SetParent(null, false);
        gameObject.SetActive(false);
        _releaseAction?.Invoke(this);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Preview Full Effect")]
    private void ContextMenuPreviewFullEffect()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        StartEditorPreview();
    }

    [ContextMenu("Debug/Stop Preview")]
    private void ContextMenuStopPreview()
    {
        StopEditorPreview(clearParticles: true);
    }

    private void Reset()
    {
        CacheParticleSystems();
    }

    private void OnValidate()
    {
        CacheParticleSystems();
        _sfxDelay = Mathf.Max(0f, _sfxDelay);
        _minimumSecondsBetweenSounds = Mathf.Max(0f, _minimumSecondsBetweenSounds);
        _leadDelayBeforeStarReveal = Mathf.Max(0f, _leadDelayBeforeStarReveal);
        _totalDuration = Mathf.Max(_leadDelayBeforeStarReveal, _totalDuration);
    }

    private void CacheParticleSystems()
    {
        if (_particleSystems == null || _particleSystems.Length == 0)
            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void StartEditorPreview()
    {
        StopEditorPreview(clearParticles: true);
        CacheParticleSystems();
        PrepareEditorPreviewParticleSystems();

        _editorPreviewStartTime = EditorApplication.timeSinceStartup;
        _editorPreviewLastTime = _editorPreviewStartTime;
        _isEditorPreviewPlaying = true;

        EditorApplication.update -= EditorPreviewUpdate;
        EditorApplication.update += EditorPreviewUpdate;
        SceneView.RepaintAll();
    }

    private void StopEditorPreview(bool clearParticles)
    {
        if (_isEditorPreviewPlaying)
            EditorApplication.update -= EditorPreviewUpdate;

        _isEditorPreviewPlaying = false;
        RestoreEditorPreviewParticleSystems();

        if (!clearParticles)
            return;

        for (int i = 0; i < _particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = _particleSystems[i];
            if (particleSystem == null)
                continue;

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(true);
            particleSystem.Simulate(0f, false, true, true);
        }

        SceneView.RepaintAll();
    }

    private void EditorPreviewUpdate()
    {
        if (!_isEditorPreviewPlaying || this == null)
        {
            StopEditorPreview(clearParticles: false);
            return;
        }

        double currentTime = EditorApplication.timeSinceStartup;
        float elapsedTime = (float)(currentTime - _editorPreviewStartTime);
        float deltaTime = (float)(currentTime - _editorPreviewLastTime);
        _editorPreviewLastTime = currentTime;

        if (deltaTime <= 0f)
            return;

        for (int i = 0; i < _particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = _particleSystems[i];
            if (particleSystem == null)
                continue;

            particleSystem.Simulate(deltaTime, false, false, true);
        }

        SceneView.RepaintAll();

        if (elapsedTime >= _totalDuration)
            StopEditorPreview(clearParticles: false);
    }

    private void PrepareEditorPreviewParticleSystems()
    {
        int particleSystemCount = _particleSystems != null ? _particleSystems.Length : 0;
        if (_editorPreviewOriginalAutoRandomSeeds == null || _editorPreviewOriginalAutoRandomSeeds.Length != particleSystemCount)
            _editorPreviewOriginalAutoRandomSeeds = new bool[particleSystemCount];
        if (_editorPreviewOriginalRandomSeeds == null || _editorPreviewOriginalRandomSeeds.Length != particleSystemCount)
            _editorPreviewOriginalRandomSeeds = new uint[particleSystemCount];

        for (int i = 0; i < particleSystemCount; i++)
        {
            ParticleSystem particleSystem = _particleSystems[i];
            if (particleSystem == null)
                continue;

            _editorPreviewOriginalAutoRandomSeeds[i] = particleSystem.useAutoRandomSeed;
            _editorPreviewOriginalRandomSeeds[i] = particleSystem.randomSeed;

            particleSystem.useAutoRandomSeed = false;
            particleSystem.randomSeed = (uint)(1001 + (i * 977));
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(true);
            particleSystem.Simulate(0f, false, true, true);
        }
    }

    private void RestoreEditorPreviewParticleSystems()
    {
        if (_particleSystems == null || _editorPreviewOriginalAutoRandomSeeds == null || _editorPreviewOriginalRandomSeeds == null)
            return;

        int particleSystemCount = Mathf.Min(_particleSystems.Length, _editorPreviewOriginalAutoRandomSeeds.Length, _editorPreviewOriginalRandomSeeds.Length);
        for (int i = 0; i < particleSystemCount; i++)
        {
            ParticleSystem particleSystem = _particleSystems[i];
            if (particleSystem == null)
                continue;

            particleSystem.useAutoRandomSeed = _editorPreviewOriginalAutoRandomSeeds[i];
            particleSystem.randomSeed = _editorPreviewOriginalRandomSeeds[i];
        }
    }
#endif

    private void OnDisable()
    {
        _releaseRoutine = null;
        _soundRoutine = null;
#if UNITY_EDITOR
        StopEditorPreview(clearParticles: false);
#endif
    }
}
