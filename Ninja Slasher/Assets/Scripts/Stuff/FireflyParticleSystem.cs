#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class FireflyParticleSystem : MonoBehaviour
{
    [Header("PARTICLES")]
    [SerializeField] private Sprite fireflySprite;
    [SerializeField] private Sprite fireflyLightSprite;

    [Header("AMOUNT")]
    [SerializeField] private int maxParticles = 30;
    [SerializeField] private float emissionRate = 2f;

    [Header("AREA")]
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(20f, 10f);

    [Header("MOVEMENT")]
    [SerializeField] private float moveSpeed = 0.5f;
    [SerializeField] private Vector2 directionChangeInterval = new Vector2(1f, 3f);
    [SerializeField] private float turbulence = 0.3f;

    [Header("SETTINGS")]
    [SerializeField] private Vector2 sizeRange = new Vector2(0.1f, 0.2f);
    [SerializeField] private Color lightColor = new Color(1f, 0.9f, 0.5f, 1f);
    [SerializeField] private Gradient colorOverLifetime;

    [Header("LIGHTNING")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private AnimationCurve pulseCurve;

    [Header("LAYER")]
    [SerializeField] private string sortingLayerName;
    [SerializeField] private int sortingOrder = 5;

    private ParticleSystem particleSystem;
    private ParticleSystemRenderer particleRenderer;

    void Start()
    {
        ConfigureParticleSystem();
    }

    private void ConfigureParticleSystem()
    {
        particleSystem = GetComponent<ParticleSystem>();
        particleRenderer = GetComponent<ParticleSystemRenderer>();

        var main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.maxParticles = maxParticles;
        main.startLifetime = new ParticleSystem.MinMaxCurve(15f, 25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(moveSpeed * 0.5f, moveSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startRotation = new ParticleSystem.MinMaxCurve(0, 360f * Mathf.Deg2Rad);
        main.startColor = lightColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;

        ConfigureEmission();
        ConfigureShape();
        ConfigureVelocity();
        ConfigureColorOverLifetime();
        ConfigureSizeOverLifetime();
        ConfigureRenderer();
    }

    private void ConfigureEmission()
    {
        var emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;
    }

    private void ConfigureShape()
    {
        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0.1f);
    }

    private void ConfigureVelocity()
    {
        var velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;

        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-moveSpeed, moveSpeed);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-moveSpeed * 0.5f, moveSpeed * 0.5f);
        velocityOverLifetime.z = 0;

        var noise = particleSystem.noise;
        noise.enabled = true;
        noise.strength = turbulence;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.2f;
        noise.octaveCount = 2;
        noise.quality = ParticleSystemNoiseQuality.Medium;
    }

    private void ConfigureColorOverLifetime()
    {
        var colorModule = particleSystem.colorOverLifetime;
        colorModule.enabled = true;

        if (colorOverLifetime == null || colorOverLifetime.colorKeys.Length == 0)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(lightColor, 0.0f),
                    new GradientColorKey(lightColor, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.1f),
                    new GradientAlphaKey(1f, 0.9f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime = gradient;
        }

        colorModule.color = new ParticleSystem.MinMaxGradient(colorOverLifetime);
    }

    private void ConfigureSizeOverLifetime()
    {
        var sizeModule = particleSystem.sizeOverLifetime;
        sizeModule.enabled = true;

        if (pulseCurve == null || pulseCurve.length == 0)
        {
            pulseCurve = new AnimationCurve();
            pulseCurve.AddKey(0f, 0.3f);
            pulseCurve.AddKey(0.25f, 1f);
            pulseCurve.AddKey(0.5f, 0.3f);
            pulseCurve.AddKey(0.75f, 1f);
            pulseCurve.AddKey(1f, 0.3f);

#if UNITY_EDITOR
            for (int i = 0; i < pulseCurve.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(pulseCurve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(pulseCurve, i, AnimationUtility.TangentMode.ClampedAuto);
            }
#endif
        }

        sizeModule.size = new ParticleSystem.MinMaxCurve(1f, pulseCurve);
        sizeModule.sizeMultiplier = 1f;
    }

    private void ConfigureRenderer()
    {
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.alignment = ParticleSystemRenderSpace.View;
        particleRenderer.sortingLayerName = sortingLayerName;
        particleRenderer.sortingOrder = sortingOrder;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        if (fireflyLightSprite != null)
        {
            Material material = new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = fireflyLightSprite.texture;
            particleRenderer.material = material;
        }
        else if (fireflySprite != null)
        {
            Material material = new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = fireflySprite.texture;
            particleRenderer.material = material;
        }
    }

    public void Play()
    {
        if (particleSystem != null)
        {
            particleSystem.Play();
        }
    }

    public void Stop()
    {
        if (particleSystem != null)
        {
            particleSystem.Stop();
        }
    }

    public void Clear()
    {
        if (particleSystem != null)
        {
            particleSystem.Clear();
        }
    }

    public void SetEmissionRate(float rate)
    {
        emissionRate = rate;
        var emission = particleSystem.emission;
        emission.rateOverTime = rate;
    }

    public void SetMaxParticles(int max)
    {
        maxParticles = max;
        var main = particleSystem.main;
        main.maxParticles = max;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0.1f));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0.1f));
    }
}