using UnityEngine;

public class Model : MonoBehaviour
{
    public float DashForce => _dashForce;
    [SerializeField] float _dashForce;
    public float DashCD => _dashCD;
    [SerializeField] float _dashCD;
    public void SetDashCD(float newValue) => _dashCD = newValue;
    public float DashDuration => _dashDuration;
    [SerializeField] float _dashDuration = 1f;

    [SerializeField] private float _parryRange = 0.5f;
    public float ParryRange => _parryRange;
    public void SetParryRange(float newValue) => _parryRange = newValue;

    [SerializeField] private float _parryCD = 1f;
    public float ParryCD => _parryCD;

    [SerializeField] private float slashEffectDuration;
    public float SlashEffectDuration => slashEffectDuration;

}
