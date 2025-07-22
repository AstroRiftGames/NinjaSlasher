using UnityEngine;

public class NanoSwarm : FlyingEnemy
{
    [SerializeField] int _childrenAmount;
    [SerializeField] GameObject _miniSwarmBot;
    public override void Die()
    {
        for (int n = 0; n < _childrenAmount; n++)
        {
            Instantiate(_miniSwarmBot, 
                        new Vector2(transform.position.x + Random.Range(-2f, 2f), transform.position.y), 
                        Quaternion.identity).TryGetComponent(out MiniSwarmBot bot);
            bot.StartCoroutine(bot.Initialize());
        }
        base.Die();
    }
}
