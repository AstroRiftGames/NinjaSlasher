using UnityEngine;

public class EnergyProjectile : Projectile
{
    [SerializeField] [Range(1, 5)] float _multiplier;
    public override void ReflectBackwards(Transform newShooter, Vector2 newDir)
    {
        MultiplySpeed(_multiplier);
        base.ReflectBackwards(newShooter, newDir);
    }
}
