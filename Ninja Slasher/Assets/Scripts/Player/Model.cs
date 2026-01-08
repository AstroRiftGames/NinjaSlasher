using UnityEngine;

public class Model : MonoBehaviour
{
    public float DashForce => _dashForce;
    [SerializeField] float _dashForce;
    public float DashCD => _dashCD;
    [SerializeField] float _dashCD;
    public float DashDuration => _dashDuration;
    [SerializeField] float _dashDuration = 1f;

    [SerializeField] private float _parryRange = 0.5f;
    public float ParryRange => _parryRange;

    [SerializeField] private float _parryCD = 1f;
    public float ParryCD => _parryCD;

}
