using UnityEngine;

public class lamp : MonoBehaviour
{
    bool _isFalling;
    [SerializeField] Rigidbody2D _rb;
    [SerializeField] Collider2D _col;


    private void Update()
    {
        _isFalling = _rb.linearVelocityY < 0;
        _col.enabled = _isFalling;
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            collision.TryGetComponent(out Controller controller);
            controller.Die();
        }

        if(collision.CompareTag("Enemy"))
        {
            collision.TryGetComponent(out Enemy enemy);
            enemy.Die();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.collider.tag is "Scenario" or "Floor" or "Obstacle" && _isFalling)
        {
            _rb.bodyType = RigidbodyType2D.Static;
        }
    }
}
