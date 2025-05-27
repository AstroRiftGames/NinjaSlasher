using UnityEngine;

public class Model : MonoBehaviour
{
    public float DashForce => _dashForce;
    [SerializeField] float _dashForce;
    public float DashCD => _dashCD;
    [SerializeField] float _dashCD;

}
