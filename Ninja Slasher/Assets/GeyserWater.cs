using UnityEngine;
using System.Collections.Generic;

public class GeyserWater : MonoBehaviour
{
    NewController player;
    ParticleSystem ps;

    private void Awake()
    {
        player = FindFirstObjectByType<NewController>();
        ps = GetComponent<ParticleSystem>();
    }
    private void OnParticleTrigger()
    {
        List<ParticleCollisionEvent> list = new List<ParticleCollisionEvent>();
        int amount = ps.GetCollisionEvents(player.gameObject, list);

        Debug.Log("Particles collided");
        if(amount > 1)
        {
            Debug.Log("Particles killed player");
            player.Die();
        }
    }
}
