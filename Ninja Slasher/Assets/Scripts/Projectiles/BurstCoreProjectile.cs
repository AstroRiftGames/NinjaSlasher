using UnityEngine;

public class BurstCoreProjectile : Projectile
{
    [SerializeField] private int _miniProjectilesAmount;
    [SerializeField] private GameObject _miniProjectilePrefab;
    [SerializeField] private float _timeToBurst;
    [SerializeField] private float _maxDispersion;
    private Vector2 _currentDirection;

    private float _startTime;

    public override void SetDirection(Vector2 direction)
    {
        _rb.AddForce(direction * _speed);
        _startTime = Time.time;
        _currentDirection = direction;
    }

    public override void Update()
    {
        if (TryHandleGameplayClosed())
            return;

        if(Time.time >= _startTime + _timeToBurst)
        {
            Burst();
        }
    }

    private void Burst()
    {
        for (int n = 1; n <= _miniProjectilesAmount; n++)
        {
            Projectile newProjectile = Instantiate(_miniProjectilePrefab, transform.position, Quaternion.identity).GetComponent<Projectile>();
            newProjectile.SetParryOverrideProvider(ParryOverrideProvider);
            newProjectile.Initialize(GetRandomDir(), _shooter);
        }
        Destroy(gameObject);
    }

    private Vector2 GetRandomDir()
    {
        return new Vector2(Random.Range(-180, 180), Random.Range(-180, 180)).normalized;
    }
}
