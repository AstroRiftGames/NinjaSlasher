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
}
