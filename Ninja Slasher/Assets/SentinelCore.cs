using UnityEngine;

public class SentinelCore : MonoBehaviour
{
    DemolitionSentinel _sentinel;
    Collider2D _collider;
    private void Awake()
    {
        TryGetComponent(out Collider2D collider);
        _collider = collider;
        _sentinel = GetComponentInParent<DemolitionSentinel>();
    }
    private void OnEnable()
    {
        _collider.enabled = true;
    }

    private void KillSentinel()
    {
        _sentinel.Die();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            KillSentinel();
        }
    }
}
