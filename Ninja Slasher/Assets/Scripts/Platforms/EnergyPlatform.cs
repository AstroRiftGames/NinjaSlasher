using UnityEngine;

public class EnergyPlatform : PlatformBase
{
    [SerializeField] private float onDuration;
    [SerializeField] private float offDuration;
    [SerializeField] private Collider2D platformCollider;
    [SerializeField] private SpriteRenderer platformRenderer;

    [SerializeField] private float onAlpha;
    [SerializeField] private float offAlpha;

    private bool isOn = true;
    private float timer;

    protected override void InitializePlatform()
    {
        timer = onDuration;
        SetPlatformState(true);
    }

    public override void OnPlatformUpdate()
    {
        timer -= Time.deltaTime;

        if (isOn && timer <= 0f)
        {
            SetPlatformState(false);
            timer = offDuration;
        }
        else if (!isOn && timer <= 0f)
        {
            SetPlatformState(true);
            timer = onDuration;
        }
    }

    private void SetPlatformState(bool state)
    {
        isOn = state;

        if (platformCollider != null)
            platformCollider.isTrigger = !isOn;

        if (platformRenderer != null)
        {
            Color color = platformRenderer.color;
            color.a = isOn ? onAlpha : offAlpha;
            platformRenderer.color = color;
        }
    }

    public override void OnPlayerEnter(GameObject player)
    {
        if (!isOn)
        {
        }
    }

    public override void OnPlayerExit(GameObject player, bool isForced = false) { }
}
