using UnityEngine;

public class NanoSwarm : FlyingEnemy
{
    [SerializeField] int _childrenAmount;
    [SerializeField] GameObject _miniSwarmBot;
    private bool _isDying = false;

    public override void Die()
    {
        if(_isDead) return;
        for (int n = 0; n < _childrenAmount; n++)
        {
            GameObject miniSwarm = Instantiate(
                _miniSwarmBot,
                new Vector2(transform.position.x + Random.Range(-3f, 3f), transform.position.y + Random.Range(-1.5f, 1.5f)),
                Quaternion.identity);

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
}
