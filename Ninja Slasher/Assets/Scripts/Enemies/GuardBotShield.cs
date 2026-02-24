using UnityEngine;

public class GuardBotShield : VulnerabilityCheck
{
    private GuardBot _bot;

    private void Awake()
    {
        _bot = GetComponentInParent<GuardBot>();
    }

    public override void ManageColision()
    {
        base.ManageColision();
        AudioService.Instance.PlaySFXAtPosition(_bot.AudioContext.Audio.collision, _bot.transform.position);
    }
}
