using UnityEngine;

public class View : MonoBehaviour
{
    public Rigidbody2D RB => _rb;
    [SerializeField] Rigidbody2D _rb;

    public Animator Animator => _anim;
    [SerializeField] Animator _anim;

    public Portal LastUsedPortal { get; set; }

    public Vector2 CurrentVelocity => _currentVelocity;
    private Vector2 _currentVelocity;

    public void SetVelocity(Vector2 newVel) => _currentVelocity = newVel;
}
