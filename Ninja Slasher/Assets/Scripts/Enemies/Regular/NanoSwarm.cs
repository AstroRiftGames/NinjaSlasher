using UnityEngine;

public class NanoSwarm : FlyingEnemy
{
    [SerializeField] int _childrenAmount;
    [SerializeField] GameObject _miniSwarmBot;
    private bool _isDying = false;

    private const float SpawnCheckRadius = 0.5f;
    private const int MaxSpawnAttempts = 10;

    public override void Die()
    {
        if(_isDead) return;
        for (int n = 0; n < _childrenAmount; n++)
        {
            Vector2 spawnPos = GetSafeSpawnPosition();
            GameObject miniSwarm = Instantiate(_miniSwarmBot, spawnPos, Quaternion.identity);

            if (!miniSwarm.TryGetComponent(out MiniSwarmBot bot))
                continue;

            LevelSessionManager.Instance?.RegisterSpawnedEnemy(bot);
            bot.StartCoroutine(bot.Initialize());
        }

        _isDying = true;
        _rb.linearVelocity = Vector2.zero;
        base.Die();
    }

    public override void CustomUpdate()
    {
        if (GameManager.Instance.PlayerHasDied) return;
        if (!_isDying)
        {
            base.CustomUpdate();
        }
    }

    private Vector2 GetSafeSpawnPosition()
    {
        for (int i = 0; i < MaxSpawnAttempts; i++)
        {
            Vector2 candidate = new Vector2(
                transform.position.x + Random.Range(-3f, 3f),
                transform.position.y + Random.Range(-1.5f, 1.5f)
            );
            Debug.DrawLine(transform.position, candidate, Color.red, 1f);
            Collider2D hit = Physics2D.OverlapCircle(candidate, SpawnCheckRadius);
            if (hit == null || (!hit.CompareTag("Obstacle") && !hit.CompareTag("Scenario")))
                return candidate;
        }

        return transform.position;
    }
}
