using UnityEngine;
using System.Collections.Generic;

public class GeyserWater : MonoBehaviour
{
    PlayerController player;
    ParticleSystem ps;

    private void Awake()
    {
        player = FindFirstObjectByType<PlayerController>();
        ps = GetComponent<ParticleSystem>();
    }
    private void OnParticleTrigger()
    {
        List<ParticleCollisionEvent> list = new List<ParticleCollisionEvent>();
        int amount = ps.GetCollisionEvents(player.gameObject, list);

        if(amount > 1)
        {
            player.Die();
        }
    }
}
