using UnityEngine;
using System;

public class Cable : MonoBehaviour
{
    [SerializeField] private Transform itemAttached;

    public Action OnDropItem;

    void Awake()
    {
        TryGetComponent(out SpriteRenderer renderer);
        renderer.size = new Vector2(renderer.size.x, Mathf.Abs(itemAttached.position.y - transform.position.y));
        TryGetComponent(out BoxCollider2D col);
        col.size = renderer.size;
        col.offset = new Vector2(0, -renderer.size.y / 2);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Cut();
        }
    }

    void Cut()
    {
        OnDropItem?.Invoke();
        itemAttached.SetParent(null);
        itemAttached.TryGetComponent(out Rigidbody2D rb);
        rb.bodyType = RigidbodyType2D.Dynamic;
        Destroy(gameObject);
    }
}
