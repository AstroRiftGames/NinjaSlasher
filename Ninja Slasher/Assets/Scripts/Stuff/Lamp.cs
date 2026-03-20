using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Lamp : MonoBehaviour
{
    bool _isFalling;
    [SerializeField] Rigidbody2D _rb;
    [SerializeField] Collider2D _col;
    [SerializeField] Animator _animator;

    [SerializeField] Cable cable;


    private void OnEnable()
    {
        cable.OnDropItem += Drop;
    }

    private void OnDisable()
    {

        cable.OnDropItem -= Drop;
    }

    private void Update()
    {
        _isFalling = _rb.linearVelocityY < 0;
        _col.enabled = _isFalling;
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(_isFalling)
        {
            if (collision.CompareTag("Player"))
            {
                collision.TryGetComponent(out NewController controller);
                controller.Die();
            }

            if (collision.CompareTag("Enemy"))
            {
                collision.TryGetComponent(out Enemy enemy);
                enemy.Die();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.tag is "Scenario" or "Floor" or "Obstacle" && _isFalling)
        {
            _rb.bodyType = RigidbodyType2D.Static;
        }
    }

    private void Drop()
    {
        _animator.SetTrigger("OnDrop");
    }
}