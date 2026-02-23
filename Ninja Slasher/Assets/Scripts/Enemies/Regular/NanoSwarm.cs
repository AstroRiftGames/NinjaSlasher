using UnityEngine;

public class NanoSwarm : FlyingEnemy
{
    [SerializeField] int _childrenAmount;
    [SerializeField] GameObject _miniSwarmBot;

    public override void Die()
    {
        //AudioManager.Instance.PlaySFXAtPosition(SFXClip.E_Nano_Death, transform.position);
        //AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.death, transform.position);
        for (int n = 0; n < _childrenAmount; n++)
        {
            Instantiate(_miniSwarmBot, 
                        new Vector2(transform.position.x + Random.Range(-3f, 3f), transform.position.y + Random.Range(-1.5f, 1.5f)), 
                        Quaternion.identity).TryGetComponent(out MiniSwarmBot bot);
            bot.StartCoroutine(bot.Initialize());
        }
        base.Die();
    }
}
