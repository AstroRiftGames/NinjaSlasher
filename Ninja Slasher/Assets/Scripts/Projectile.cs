using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float _speed;
    private Transform _target;
    private Rigidbody2D _rb;

    private void OnEnable()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 direction)
    {
        _rb.AddForce(direction * _speed);
    }

    public void SetTarget(Transform newTarget) => _target = newTarget;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            Debug.Log("Game Over");
            Destroy(collision.gameObject);
        }
        Destroy(gameObject);
    }
}
