using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject.Asteroids;

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
    [SerializeField] Surface _currentSurface = Surface.Floor;
    [SerializeField] private LayerMask Scenariolayer;
    //[SerializeField] Transform _body;

    //[SerializeField] bool _movingRight;
    //[SerializeField] bool _movingLeft;
    //[SerializeField] float _speed;

    //bool rightSurface;
    //bool leftSurface;
    //bool _rotating;

    [SerializeField] GameObject _sprites;

    [Header("Attack Parameters")]
    private float _attackCD = 3f;
    private float _lastAttack = 0;
    [SerializeField] float _minAttackCD;
    [SerializeField] float _maxAttackCD;

    [Header("SpawnAttack Parameters")]
    [SerializeField] [Range(0f,1f)] float _spawnAttackChance;
    [SerializeField] GameObject _blaztEgg;
    [SerializeField] int _blaztEggsAmount;
    [SerializeField] float _launchingBaseForce;


    [Header("FurtiveAttack Parameters")]
    [SerializeField] float _hidingTime;

    private void Update()
    {
        //if(!_rotating) _currentSurface = CheckSurfaceChange();
        //UpdateSurface();
        //Move();

        if (CheckAttackCooldown()) PrepareAttack();
    }


    #region ATTACK LOGIC
    private void PrepareAttack()
    {
        _lastAttack = Time.time;
        SetCD();
        Attack();
    }

    private void SetCD() => _attackCD = Random.Range(_minAttackCD, _maxAttackCD);

    private void Attack()
    {
        float r = Random.Range(0f, 1f);
        float chance = _spawnAttackChance; //TODO: Adjust by amount of instantiated BLAZT's
        if (r <= chance) SpawnAttack();
        else StartCoroutine(FurtiveAttack());
    }

    #region SPAWN ATTACK
    private void SpawnAttack()
    {
        for(int n = 0; n < _blaztEggsAmount; n++)
        {
            Instantiate(_blaztEgg, transform.position + transform.up, Quaternion.identity).TryGetComponent(out Rigidbody2D eggRB);
            eggRB.AddForce(SetDirection(n), ForceMode2D.Impulse);
        }
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
        transform.position = closestPoint;
        yield return new WaitForSeconds(_hidingTime-.5f);
        _sprites.SetActive(true);

        yield return new WaitForSeconds(.25f);
        _animator.SetTrigger("OnEmerge");
        _col.enabled = true;
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
                transform.rotation = Quaternion.Euler(0, 0, 180);
                _currentSurface = Surface.Ceiling;
            }
        }
    }

    #endregion

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.TryGetComponent(out Controller player);
            player.Die();
            Debug.Log("Player Killed");
        }
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
            RaycastHit2D hit = Physics2D.Raycast(origin, dirToCast, 15, Scenariolayer);
            Debug.DrawRay(origin, dirToCast * 15, Color.yellow, 1);
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

    //private void Move()
    //{
    //    _rb.linearVelocity = _body.right * (_movingLeft ? -1 : (_movingRight ? 1 :0) * _speed);
    //}

    //private Surface CheckSurfaceChange()
    //{
    //    rightSurface = Physics2D.Raycast(_body.position + _body.right/2, _body.right, .5f, ScenarioLayer);
    //    leftSurface = Physics2D.Raycast(_body.position + _body.right*-1/2, _body.right*-1, .5f, ScenarioLayer);

    //    Surface targetSurface = _currentSurface;
    //    if (_movingRight && rightSurface)
    //    {
    //        targetSurface = _currentSurface switch
    //        {
    //            Surface.Floor => Surface.Right_Wall,
    //            Surface.Right_Wall => Surface.Ceiling,
    //            Surface.Ceiling => Surface.Left_Wall,
    //            Surface.Left_Wall => Surface.Floor,
    //            _ => Surface.None,
    //        };

    //    }
    //    else if (_movingLeft && leftSurface)
    //    {
    //        targetSurface = _currentSurface switch
    //        {
    //            Surface.Floor => Surface.Left_Wall,
    //            Surface.Left_Wall => Surface.Ceiling,
    //            Surface.Ceiling => Surface.Right_Wall,
    //            Surface.Right_Wall => Surface.Floor,
    //            _ => Surface.None,
    //        };
    //    }
    //    if (targetSurface != Surface.None) _rotating = true;
    //    return targetSurface;
    //}


    //private void UpdateSurface()
    //{
    //    Vector2 dirToLook = Vector2.up;

    //    dirToLook = _currentSurface switch
    //    {
    //        Surface.Floor => Vector2.up,
    //        Surface.Right_Wall => Vector2.left,
    //        Surface.Ceiling => Vector2.down,
    //        Surface.Left_Wall => Vector2.right,
    //        _ => Vector2.up
    //    };

    //    _body.rotation = Quaternion.Euler(dirToLook);
    //}

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.up * 3);
        //Gizmos.DrawRay(_body.position + _body.right * -1 /2, _body.right * -1 * .5f);
        //Gizmos.DrawRay(_body.position + _body.right /2, _body.right * .5f);
    }
}
