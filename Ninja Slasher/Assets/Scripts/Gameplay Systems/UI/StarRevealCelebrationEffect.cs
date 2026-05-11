using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(ParticleSystemRenderer))]
public sealed class StarRevealCelebrationEffect : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float _leadDelayBeforeStarReveal = 0.12f;
    [SerializeField] private float _duration = 0.18f;
    [SerializeField] private float _lifetimeMin = 0.24f;
    [SerializeField] private float _lifetimeMax = 0.42f;

    [Header("Burst")]
    [SerializeField] private int _burstCount = 26;
    [SerializeField] private float _shapeRadius = 6f;
    [SerializeField] private float _startSpeedMin = 48f;
    [SerializeField] private float _startSpeedMax = 84f;
    [SerializeField] private float _startSizeMin = 12f;
    [SerializeField] private float _startSizeMax = 22f;
    [SerializeField] private float _rotationSpeedMin = -180f;
    [SerializeField] private float _rotationSpeedMax = 180f;

    [Header("Rendering")]
    [SerializeField] private Sprite _particleSprite;
    [SerializeField] private int _sortingOrderOffset = 1;

    private ParticleSystem _particleSystem;
    private ParticleSystemRenderer _particleRenderer;
    private Material _runtimeMaterial;
    private Sprite _appliedSprite;
    private Action<StarRevealCelebrationEffect> _releaseAction;
    private Coroutine _releaseRoutine;

    public float LeadDelayBeforeStarReveal => _leadDelayBeforeStarReveal;
    public float TotalDuration => _duration + _lifetimeMax + 0.05f;

    public void Initialize(Action<StarRevealCelebrationEffect> releaseAction)
    {
        _releaseAction = releaseAction;
        ResolveComponents();
        ConfigureParticleSystem();
    }

    public void Play(Transform parent, Vector3 localPosition, Sprite particleSprite, Canvas sourceCanvas, float scaleMultiplier)
    {
        ResolveComponents();
        ApplySprite(particleSprite != null ? particleSprite : _particleSprite);
        ApplySorting(sourceCanvas);
        ConfigureParticleSystem();

        transform.SetParent(parent, false);
        transform.localPosition = localPosition;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one * scaleMultiplier;

        gameObject.SetActive(true);
        _particleSystem.Clear(true);
        _particleSystem.Play(true);

        if (_releaseRoutine != null)
            StopCoroutine(_releaseRoutine);

        _releaseRoutine = StartCoroutine(ReleaseAfterPlayback());
    }

    public void StopAndRelease()
    {
        ResolveComponents();

        if (_releaseRoutine != null)
        {
            StopCoroutine(_releaseRoutine);
            _releaseRoutine = null;
        }

        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _particleSystem.Clear(true);
        ReleaseToPool();
    }

    private IEnumerator ReleaseAfterPlayback()
    {
        yield return new WaitForSecondsRealtime(TotalDuration);
        _releaseRoutine = null;
        ReleaseToPool();
    }

    private void ReleaseToPool()
    {
        transform.SetParent(null, false);
        gameObject.SetActive(false);
        _releaseAction?.Invoke(this);
    }

    private void ResolveComponents()
    {
        if (_particleSystem == null)
            _particleSystem = GetComponent<ParticleSystem>();

        if (_particleRenderer == null)
            _particleRenderer = GetComponent<ParticleSystemRenderer>();
    }

    private void ConfigureParticleSystem()
    {
        ResolveComponents();

        var main = _particleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.useUnscaledTime = true;
        main.duration = _duration;
        main.maxParticles = Mathf.Max(_burstCount, 8);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(_lifetimeMin, _lifetimeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(_startSpeedMin, _startSpeedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(_startSizeMin, _startSizeMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.9f, 0.25f, 1f),
            new Color(0.25f, 0.95f, 1f, 1f));

        var emission = _particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)_burstCount) });

        var shape = _particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = _shapeRadius;
        shape.radiusThickness = 0f;
        shape.arc = 360f;

        var velocityOverLifetime = _particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.speedModifier = new ParticleSystem.MinMaxCurve(1f, BuildCurve(1f, 0.28f));

        var limitVelocity = _particleSystem.limitVelocityOverLifetime;
        limitVelocity.enabled = true;
        limitVelocity.space = ParticleSystemSimulationSpace.Local;
        limitVelocity.dampen = 0.55f;
        limitVelocity.limit = new ParticleSystem.MinMaxCurve(140f);

        var sizeOverLifetime = _particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, BuildCurve(1f, 0.2f));

        var colorOverLifetime = _particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(BuildColorGradient());

        var rotationOverLifetime = _particleSystem.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.separateAxes = false;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(_rotationSpeedMin * Mathf.Deg2Rad, _rotationSpeedMax * Mathf.Deg2Rad);

        var noise = _particleSystem.noise;
        noise.enabled = true;
        noise.strength = 5f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.6f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var trails = _particleSystem.trails;
        trails.enabled = false;

        _particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        _particleRenderer.alignment = ParticleSystemRenderSpace.View;
        _particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _particleRenderer.receiveShadows = false;
        _particleRenderer.allowOcclusionWhenDynamic = false;
        _particleRenderer.enableGPUInstancing = false;
    }

    private void ApplySorting(Canvas sourceCanvas)
    {
        ResolveComponents();

        if (sourceCanvas == null)
            return;

        _particleRenderer.sortingLayerID = sourceCanvas.sortingLayerID;
        _particleRenderer.sortingOrder = sourceCanvas.sortingOrder + _sortingOrderOffset;
    }

    private void ApplySprite(Sprite sprite)
    {
        ResolveComponents();

        if (sprite == null || _appliedSprite == sprite && _runtimeMaterial != null)
            return;

        _appliedSprite = sprite;

        if (_runtimeMaterial == null)
        {
            Shader spriteShader = Shader.Find("Sprites/Default");
            _runtimeMaterial = new Material(spriteShader != null ? spriteShader : Shader.Find("Universal Render Pipeline/Unlit"));
            _runtimeMaterial.name = $"{nameof(StarRevealCelebrationEffect)}RuntimeMaterial";
        }

        _runtimeMaterial.mainTexture = sprite.texture;
        _particleRenderer.material = _runtimeMaterial;
    }

    private static AnimationCurve BuildCurve(float startValue, float endValue)
    {
        return AnimationCurve.EaseInOut(0f, startValue, 1f, endValue);
    }

    private static Gradient BuildColorGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 1f, 1f, 1f), 0f),
                new GradientColorKey(new Color(1f, 0.8f, 0.35f, 1f), 0.32f),
                new GradientColorKey(new Color(1f, 0.3f, 0.65f, 1f), 0.68f),
                new GradientColorKey(new Color(0.35f, 0.95f, 1f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(0.92f, 0.72f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    private void OnDestroy()
    {
        if (_runtimeMaterial != null)
            Destroy(_runtimeMaterial);
    }
}
