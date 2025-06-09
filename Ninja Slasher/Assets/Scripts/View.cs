using UnityEngine;

public class View : MonoBehaviour
{
    public Rigidbody2D RB => _rb;
    [SerializeField] Rigidbody2D _rb;

    public Portal LastUsedPortal { get; set; }

    public Vector2 CurrentVelocity;
}
