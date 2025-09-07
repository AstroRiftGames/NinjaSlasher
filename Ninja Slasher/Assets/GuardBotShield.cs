using UnityEngine;

public class GuardBotShield : VulnerabilityCheck
{
    private GuardBot _bot;
    private Collider2D _col;

    private void Awake()
    {
        _bot = GetComponentInParent<GuardBot>();
        TryGetComponent(out Collider2D col);
        _col = col;
    }

    private void Update()
    {
        _col.isTrigger = _bot != null && _bot.IsPushing;
    }
}
