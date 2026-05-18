using UnityEngine;

public class NanoSwarm : FlyingEnemy
{
    [SerializeField] int _childrenAmount;
    [SerializeField] GameObject _miniSwarmBot;
    private bool _isDying = false;

    private const float SpawnCheckRadius = 1f;
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
                transform.position.x + Random.Range(-2f, 2f),
                transform.position.y + Random.Range(-2f, 2f)
            );
            Collider2D hit = Physics2D.OverlapCircle(candidate, SpawnCheckRadius, _obstaclesLayer);
            if (hit == null || (!hit.CompareTag("Obstacle") && !hit.CompareTag("Scenario")))
                return candidate;
        }

        return transform.position + new Vector3(Random.Range(-.5f, .5f), Random.Range(-.5f, .5f), transform.position.z);
    }
}
