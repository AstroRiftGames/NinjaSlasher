using System.Collections;
using UnityEngine;

public class SentinelCore : BossCore
{
    private DemolitionSentinel _sentinel;

    protected override void Awake()
    {
        base.Awake();
        _sentinel = _boss as DemolitionSentinel;
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
        _sentinel.Die();
        _sentinel.enabled = false;
    }

    protected override void OnVulnerableHit(Collider2D collision)
    {
        if (_sentinel.Animator != null)
        {
            _sentinel.Animator.SetTrigger("onHit");
        }
        if (_sentinel.SentinelAudio != null)
        {
            StartCoroutine(_sentinel.SentinelAudio.DefeatedFeedbackSequence(3f));
        }
        KillSentinel();
    }
}
