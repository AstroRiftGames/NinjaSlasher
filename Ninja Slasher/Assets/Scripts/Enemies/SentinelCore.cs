using System.Collections;
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
    private void OnDisable()
    {
        _collider.enabled = false;

    }

    private void KillSentinel()
    {
        _sentinel.StopAllCoroutines();
        for (int n = 0; n < _sentinel.Balls.Length; n++)
        {
            _sentinel.Balls[n].Chain.StopAllCoroutines();
            _sentinel.Balls[n].StopAllCoroutines();
            _sentinel.Balls[n].Chain.enabled = false;
            _sentinel.Balls[n].enabled = false;
        }
        _sentinel.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _sentinel.Animator.SetTrigger("onHit");
            StartCoroutine(_sentinel.SentinelAudio.DefeatedFeedbackSequence(3f));
            KillSentinel();
        }
    }
}
