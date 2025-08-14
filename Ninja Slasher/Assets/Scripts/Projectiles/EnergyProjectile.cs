using UnityEngine;

public class EnergyProjectile : Projectile
{
    int _parriedTimes;
    [SerializeField] int _maxParries;
    [SerializeField] [Range(1, 5)] float _multiplier;
    public override void ReflectBackwards()
    {
        MultiplyReflectedSpeed(_multiplier);
        base.ReflectBackwards();
        _parriedTimes++;
        if (_parriedTimes >= _maxParries)
        {
            SetIsParryable(false);
            TryGetComponent(out SpriteRenderer sp);
            sp.color = Color.red;
        }
    }
}
