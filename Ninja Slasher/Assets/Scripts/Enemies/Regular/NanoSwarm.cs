using UnityEngine;

public class NanoSwarm : FlyingEnemy
{
    [SerializeField] int _childrenAmount;
    [SerializeField] GameObject _miniSwarmBot;
    private bool _isDying = false;

    public override void Die()
    {
        for (int n = 0; n < _childrenAmount; n++)
        {
            Instantiate(_miniSwarmBot, 
                        new Vector2(transform.position.x + Random.Range(-3f, 3f), transform.position.y + Random.Range(-1.5f, 1.5f)), 
                        Quaternion.identity).TryGetComponent(out MiniSwarmBot bot);
            bot.StartCoroutine(bot.Initialize());
        }
        _isDying = true;
        _rb.linearVelocity = Vector2.zero;
        base.Die();
    }

    public override void CustomUpdate()
    {
        if(!_isDying)
        {
            base.CustomUpdate();
        }
    }
}
