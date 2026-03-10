using System;
using UnityEngine;

[Serializable]
public class EnemyBrokenPart
{
    public Collider2D Col;
    public Rigidbody2D RB;
    [Space]

    [MinMaxSlider(-1f, 1f)]
    public FloatRange _impulseX;

    [MinMaxSlider(-1f, 1f)]
    public FloatRange _impulseY;

    [MinMaxSlider(1f, 10f)]
    public FloatRange _impulseForce;

    [MinMaxSlider(1f, 10f)]
    public FloatRange _torqueForce;


    public void BreakAndThrow()
    {
        Debug.Log("Breaking part");
        Col.enabled = true;
        RB.bodyType = RigidbodyType2D.Dynamic;
        Vector2 dir = Vector2.zero;
        dir.x = _impulseX.GetRandom();
        dir.y = _impulseY.GetRandom();
        RB.AddForce(dir*_impulseForce.GetRandom(), ForceMode2D.Impulse);
        RB.AddTorque(_torqueForce.GetRandom(), ForceMode2D.Impulse);
    }
}
