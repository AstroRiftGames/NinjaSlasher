using System;
using UnityEngine;

[Serializable]
public class EnemyBrokenPart
{
    public Collider2D Col;
    public Rigidbody2D RB;


    public void BreakAndThrow()
    {
        Debug.Log("Breaking part");
        Col.enabled = true;
        RB.bodyType = RigidbodyType2D.Dynamic;
        Vector2 dir = Vector2.zero;
        dir.x = UnityEngine.Random.Range(-1f, 1f);
        dir.y = UnityEngine.Random.Range(1f, 2f);
        float force = UnityEngine.Random.Range(3f, 8f);
        Debug.Log($"Applying force {dir * force} to {Col.gameObject.name}");
        RB.AddForce(dir*force, ForceMode2D.Impulse);
    }
}
