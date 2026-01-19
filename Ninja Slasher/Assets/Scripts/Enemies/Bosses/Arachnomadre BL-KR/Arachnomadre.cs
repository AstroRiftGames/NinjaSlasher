using Managers;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using AstroRift.Core.Pooling;

public enum Surface
{
    None,
    Ceiling,
    Floor,
    Left_Wall,
    Right_Wall,
}

public class Arachnomadre : BossEnemy
{
    [SerializeField] private Surface _currentSurface = Surface.None;

    [SerializeField] private GameObject _sprites;
    [SerializeField] private Transform _body;

    [Header("Movement Parameters")]
    [SerializeField] private float _speed;
    [SerializeField][Range(0f, 1f)] private float _dirChangeChance;
    private bool _movingRight;

    [Header("Attack Parameters")]
    private bool _isAttacking;
    private float _attackCD = 3f;
    private float _lastAttack = 0;
    [SerializeField] private float _minAttackCD;
    [SerializeField] private float _maxAttackCD;

    [Header("SpawnAttack Parameters")]
    [SerializeField] private float _launchingBaseForce;
    [SerializeField] private float _timeBetweenEggs;
    [SerializeField] [Range(0f,1f)] private float _spawnAttackChance;
    [SerializeField] private BlaztEgg _blaztEgg;
    [SerializeField] private int _blaztEggsAmount;
    private ObjectPool<BlaztEgg> _pool;
    public ObjectPool<BlaztEgg> Pool => _pool;
    private int _blaztsAmount;
    public void DecreaseEggsAmount() => _blaztsAmount--;
    public void IncreaseEggsAmount() => _blaztsAmount++;

    [Header("FurtiveAttack Parameters")]
    [SerializeField] private float _hidingTime;

    [Header("Vulnerability Parameters")]
    [SerializeField] float _vulnerabilityTime;
    private bool _isVulnerable;

    public override void Awake()
    {
        base.Awake();
        _pool = new ObjectPool<BlaztEgg>(_blaztEgg, _blaztEggsAmount, transform);
    }

    public override void CustomUpdate()
    {
        if (!_isVulnerable) 
        {
            if (CheckAttackCooldown())
            {
                PrepareAttack();
                _animator.SetBool("IsMoving", false);
            }
            else if(!_isAttacking)
            {
                _animator.SetBool("IsMoving", true);
                CheckSurface();
                Move();
            }
        }
    }

    #region MOVEMENT LOGIC
    private void Move()
    {
        transform.position += transform.right * (_movingRight ? 1:-1) * _speed * Time.deltaTime;
    }

    private void CheckSurface()
    {
        bool _isNearSurface = Physics2D.Raycast(transform.position + transform.right * (_movingRight ? 1 : -1), transform.right * (_movingRight ? 1 : -1), .25f, _obstaclesLayer);

#if UNITY_EDITOR
        Debug.DrawRay(transform.position + transform.right * (_movingRight ? 1 : -1), transform.right * (_movingRight ? 1 : -1) * .25f);
#endif

        if (_isNearSurface) Rotate();
    }

    private void Rotate()
    {
        transform.Rotate(new Vector3(0, 0, (_movingRight ? 90 : -90)));
        _animator.SetTrigger($"OnRotation{(_movingRight ? "Right" : "Left")}");
    }

    #endregion

    #region ATTACK LOGIC
    private void PrepareAttack()
    {
        _isAttacking = true;
        _lastAttack = Time.time;
        SetCD();
        SetMovementDirection();
        Attack();
    }

    private void SetMovementDirection()
    {
        float r = Random.Range(0f, 1f);
        if (r < _dirChangeChance) _movingRight = !_movingRight;
    }

    private void SetCD() => _attackCD = Random.Range(_minAttackCD, _maxAttackCD);

    private void Attack()
    {
        float r = Random.Range(0f, 1f);
        float chance = _spawnAttackChance / _blaztsAmount;
        if (r <= chance) StartCoroutine(SpawnAttack());
        else StartCoroutine(FurtiveAttack());
    }

    #region SPAWN ATTACK
    private IEnumerator SpawnAttack()
    {
        for(int n = 0; n < _blaztEggsAmount; n++)
        {
            BlaztEgg newEgg = _pool.Get();

            newEgg.transform.SetPositionAndRotation(
                transform.position + transform.up,
                Quaternion.identity
            );

            newEgg.TryGetComponent(out Rigidbody2D eggRB);
            eggRB.AddForce(SetDirection(n), ForceMode2D.Impulse);

            newEgg.SetArachnomadre(this);

            newEgg.OnRequestDespawn -= HandleEggDespawn;
            newEgg.OnRequestDespawn += HandleEggDespawn;

            yield return new WaitForSeconds(_timeBetweenEggs);
        }
        _isAttacking = false;
    }

    private void HandleEggDespawn(BlaztEgg egg)
    {
        egg.OnRequestDespawn -= HandleEggDespawn;
        _pool.Release(egg);
    }

    private Vector2 SetDirection(int index)
    {
        Vector2 dir = transform.up;
        float force = _launchingBaseForce;
        if (_currentSurface is Surface.Ceiling or Surface.Floor)
        {
            dir.x -= index switch
            {
                0 => Random.Range(0, 5f),
                1 => Random.Range(-2f, 2f),
                2 => Random.Range(-5, 0),
                _ => Random.Range(-2f, 2f),
            };
        }
        else
        {
            force *= index switch
            {
                0 => Random.Range(.25f, .75f),
                1 => Random.Range(1, 1.5f),
                2 => Random.Range(1.75f, 2.25f),
                _ => Random.Range(.25f, .75f),
            };
        }
            return dir.normalized * force;
    }
    #endregion

    #region FURTIVE ATTACK
    private IEnumerator FurtiveAttack()
    {
        _col.enabled = false;
        _animator.SetTrigger("OnSubmerge");
        yield return new WaitForSeconds(.25f);

        _sprites.SetActive(false);
        Vector2 closestPoint = GetClosestPoint(_player.position);
        Turn(closestPoint);
        transform.position = closestPoint + (Vector2)transform.up*.5f;
        
        yield return new WaitForSeconds(_hidingTime-1);

        _sprites.SetActive(true);
        _animator.SetTrigger("OnEmerge");
        _col.enabled = true;

        yield return new WaitForSeconds(.75f);
        _isAttacking = false;
    }

    private void Turn(Vector2 point)
    {
        if (point.x < _player.transform.position.x)
        {
            transform.rotation = Quaternion.Euler(0, 0, -90);
            _currentSurface = Surface.Left_Wall;
        }
        else if(point.x > _player.transform.position.x)
        {
            transform.rotation = Quaternion.Euler(0, 0, 90);
            _currentSurface = Surface.Right_Wall;
        }
        else
        {
            if(point.y < _player.transform.position.y)
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
                _currentSurface = Surface.Floor;
            }
            else if(point.y > _player.transform.position.y)
            {
                transform.rotation = Quaternion.Euler(0,0, 180);
                _currentSurface = Surface.Ceiling;
            }
        }
    }

    #endregion

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (_isVulnerable)
            {
                Die();
            }
            else
            {
                collision.gameObject.TryGetComponent(out NewController player);
                player.Die();
            }
        }
    }
    #endregion

    #region VULNERABILITY MANAGEMENT
    private bool SetVulnerability(bool value) => _isVulnerable = value;

    public IEnumerator GetVulnerable()
    {
        SetVulnerability(true);
        Debug.Log("Is now vulnerable");
        yield return new WaitForSeconds(_vulnerabilityTime);
        SetVulnerability(false);
        Debug.Log("Is no longer vulnerable");
    }
    #endregion

    #region RESOURCES
    private bool CheckAttackCooldown() => Time.time >= _attackCD + _lastAttack;
    private Vector2 GetClosestPoint(Vector2 origin)
    {
        Vector2 closestPoint = origin;
        float disToClosestSurface = float.MaxValue;

        for(int n = 0; n < 4; n++)
        {
            Vector2 dirToCast = GetDirectionByIndex(n);
            RaycastHit2D hit = Physics2D.Raycast(origin, dirToCast, 15, _obstaclesLayer);
            if(hit != false) Debug.DrawLine(origin, hit.point, Color.red, 1f);
            float disToCurrent = Vector2.Distance(origin, hit.point);

            if(disToClosestSurface == 0 || disToClosestSurface > disToCurrent)
            {
                disToClosestSurface = disToCurrent;
                closestPoint = hit.point;
            }
        }
        return closestPoint;
    }

    private Vector2 GetDirectionByIndex(int index)
    {
        return index switch
        {
            0 => Vector2.right,
            1 => Vector2.left,
            2 => Vector2.up,
            3 => Vector2.down,
            _ => throw new System.IndexOutOfRangeException(),
        };
    }
    #endregion
}
