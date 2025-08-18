using UnityEngine;

public class View : MonoBehaviour
{
    public Rigidbody2D RB => _rb;
    [SerializeField] Rigidbody2D _rb;

    public Collider2D Col => _col;
    [SerializeField] Collider2D _col;

    public Animator Animator => _anim;
    [SerializeField] Animator _anim;

    public GameObject SpriteContainer => _spriteContainer;
    [SerializeField] GameObject _spriteContainer;

    public Portal LastUsedPortal { get; set; }
}
