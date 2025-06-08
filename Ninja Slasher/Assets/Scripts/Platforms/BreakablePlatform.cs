using UnityEngine;

public class BreakablePlatform : PlatformBase
{
    [Header("SETTINGS")]
    [SerializeField] private int maxUses;

    private int remainingUses;

    protected override void InitializePlatform()
    {
        remainingUses = maxUses;
    }

    public override void OnPlayerEnter(GameObject player)
    {
        if (!isActive) return;

        remainingUses--;

        if (remainingUses <= 0)
        {
            Break();
        }
    }

    public override void OnPlayerExit(GameObject player)
    {
    }

    private void Break()
    {
        isActive = false;

        Destroy(gameObject);
    }
}
