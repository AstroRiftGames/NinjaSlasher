using System.Collections;
using TMPro;
using UnityEngine;

public class MiniSwarmBot : FlyingEnemy
{
    [SerializeField] float _deploymentTime;

    public IEnumerator Initialize()
    {
        yield return new WaitForSeconds(_deploymentTime);
        _col.enabled = true;
    }
    public override void Die()
    {
        //AudioManager.Instance.PlaySFXAtPosition(SFXClip.E_Mini_Death, transform.position);
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.death, transform.position);
        base.Die();
    }
}
