using CandyCoded.HapticFeedback;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public enum NinjaStates
{
    Idle,
    Dash,
    Grab,
    KO
}

public class NewController : MonoBehaviour
{
    public View View => _view;
    [SerializeField] View _view;
    public Model Model => _model;
    [SerializeField] Model _model;

    [SerializeField] SwipeDetection _swipeDetection;
    [SerializeField] TrajectoryRenderer _trajectoryRenderer;
    [SerializeField] private GameObject _slashVFX;

    [Header("Audio")]
    [SerializeField] private PlayerAudioSet _audio;


    public bool IsDashing => _isDashing;
    private bool _isDashing = false;
    public bool IsParrying => _isParrying;
    private bool _isParrying = false;
    private bool _isKO = false;

    private float _lastParry;
    public Vector2 LastMoveDirection => _lastMoveDirection;
    private Vector2 _lastMoveDirection;
    private Vector2 _wishedDirection = Vector2.zero;
    private float minRange = 0;
    private float maxRange = 180;
    public void SetLastMoveDirection(Vector2 dir)
    {
        _lastMoveDirection = dir;
    }
    private float _lastDash;
    private Vector2 _lastNormal = Vector2.up;
    public Vector2 LastNormal => _lastNormal;
    private PlatformBase _currentPlatform;

    [SerializeField] private LayerMask _proyectilesLayer;
    [SerializeField] private LayerMask _obstaclesLayer;

    private string[] colMatrix = { "Obstacle", "Scenario", "Floor"};
    private string[] deadlyMatrix = { "Enemy", "Spikes", "EnemyShield", };

    public Action<bool> OnHit;
    public Action<bool> OnParry;

    private void SetFlipped(float angle)
    {
        bool isFlipped = (angle > -180 && angle <= -90) || angle <= 180 && angle > 90;

        Vector2 newScale = _view.SpriteContainer.transform.localScale;
        newScale.y = isFlipped ? -Mathf.Abs(newScale.y) : Mathf.Abs(newScale.y);

        _view.SpriteContainer.transform.localScale = newScale;
    }

    private void SetMirrored(bool newValue)
    {
        Vector2 newScale = _view.SpriteContainer.transform.localScale;
        newScale.x = newValue ? -Mathf.Abs(newScale.x) : Mathf.Abs(newScale.x);

        _view.SpriteContainer.transform.localScale = newScale;
    }


    #region FSM and Behavior Tree Setup
    //private FSM<NinjaStates> _fsm;
    //private ITreeNode _root;

    //private void InitializeFSM()
    //{
    //    var idleState = new NinjaIdleState<NinjaStates>();
    //    var dashState = new NinjaDashState<NinjaStates>();
    //    var grabState = new NinjaGrabState<NinjaStates>();
    //    var koState = new NinjaKOState<NinjaStates>();

    //    idleState.AddTransition(NinjaStates.Dash, dashState);
    //    idleState.AddTransition(NinjaStates.KO, koState);
    //    dashState.AddTransition(NinjaStates.Grab, grabState);
    //    dashState.AddTransition(NinjaStates.KO, koState);
    //    grabState.AddTransition(NinjaStates.Dash, dashState);
    //    grabState.AddTransition(NinjaStates.KO, koState);
    //    koState.AddTransition(NinjaStates.Idle, idleState);

    //    _fsm = new FSM<NinjaStates>(idleState);
    //}

    //private void InitializeTree()
    //{
    //    ITreeNode idle = new ActionNode(() => { _fsm.Transition(NinjaStates.Idle); });
    //    ITreeNode dash = new ActionNode(() => { _fsm.Transition(NinjaStates.Dash); });
    //    ITreeNode grab = new ActionNode(() => { _fsm.Transition(NinjaStates.Grab); });
    //    ITreeNode ko = new ActionNode(() => { _fsm.Transition(NinjaStates.KO); });

    //    ITreeNode rootQ = new QuestionNode(QKO, ko,
    //                      new QuestionNode(QGrab, grab,
    //                      new QuestionNode(QDash, dash, idle)));
    //    _root = rootQ;
    //}


    //private bool QKO() => false;
    //private bool QGrab() => false;
    //private bool QDash() => false;

    #endregion

    #region MAGIC METHODS
    //private void Awake()
    //{
    //    //InitializeFSM();
    //    //InitializeTree(); 
    //    _swipeDetection.OnSwipe += context => { TryDash(context); };
    //    _swipeDetection.OnTap += context => { TryParry(context); };
    //}

    private void OnEnable()
    {
        if (_swipeDetection == null) return;

        _swipeDetection.OnSwipe += TryDash;
        _swipeDetection.OnInputStart += CalculateAngleRange;
        _swipeDetection.OnTap += TryParry;
    }

    private void OnDisable()
    {
        if (_swipeDetection == null) return;

        _swipeDetection.OnSwipe -= TryDash;
        _swipeDetection.OnInputStart -= CalculateAngleRange;
        _swipeDetection.OnTap -= TryParry;
    }

    private Vector2 GetFinalDirection(Vector2 startDir)
    {
        Vector2 finalDir = startDir;
        float angle = Mathf.Atan2(startDir.y, startDir.x) * Mathf.Rad2Deg;

        float clampedAngle = ClampAngle(angle, minRange, maxRange);

        float radians = clampedAngle * Mathf.Deg2Rad;

        finalDir.x = Mathf.Cos(radians);
        finalDir.y = Mathf.Sin(radians);
        return finalDir;
    }

    float ClampAngle(float angle, float min, float max)
    {
        angle = NormalizeAngle(angle);
        min = NormalizeAngle(min);
        max = NormalizeAngle(max);

        float range = Mathf.DeltaAngle(min, max);
        float delta = Mathf.DeltaAngle(min, angle);

        if (range >= 0)
        {
            if (delta >= 0 && delta <= range)
                return angle;
        }
        else
        {
            if (delta <= 0 && delta >= range)
                return angle;
        }

        float distToMin = Mathf.Abs(Mathf.DeltaAngle(angle, min));
        float distToMax = Mathf.Abs(Mathf.DeltaAngle(angle, max));

        return distToMin < distToMax ? min : max;
    }

    private void Update()
    {
        if (_swipeDetection.IsPressing)
        {
            _trajectoryRenderer.ShowTrajectory(transform.position, GetFinalDirection(_swipeDetection.Direction));
        }
        else
        {
            _trajectoryRenderer.HideTrajectory();
        }
    }
    #endregion

    #region MECHANICS

    public void ForceDash(Vector2 direction)
    {
        if (!_isKO)
        {
            Dash(direction);
        }
    }
    private void TryDash(Vector2 direction)
    {
        if (!_isKO && !_isDashing && !_isParrying && CheckDashCD())
        {
            Dash(GetFinalDirection(_swipeDetection.Direction));
        }
    }
    private void Dash(Vector2 direction)
    {
        Vector2 dashDir = direction;

        float angle = Mathf.Atan2(dashDir.y, dashDir.x) * Mathf.Rad2Deg - Mathf.Atan2(_lastNormal.y, _lastNormal.x) * Mathf.Rad2Deg;

        if(_currentPlatform != null)
        {
            _currentPlatform.OnPlayerExit(gameObject, true);
            _view.RB.gravityScale = 0;
            _currentPlatform = null;
        }

        switch (_lastNormal)
        {
            case Vector2 up when up == Vector2.up:
                if (angle is > 90 and < 180)
                {
                    dashDir = -transform.right;

                }
                else if (angle is > -180 and < -90)
                {
                    dashDir = transform.right;
                }
                break;
            case Vector2 down when down == Vector2.down:
                if (angle is > 90 and < 180)
                {
                    dashDir = transform.right;
                }
                else if (angle is > -180 and < -90)
                {
                    dashDir = transform.right;
                }
                break;
            case Vector2 right when right == Vector2.right:
                if (angle is > 90 and < 180)
                {
                    dashDir = transform.up;
                }
                else if (angle is > -180 and < -90)
                {
                    dashDir = -transform.up;
                }
                break;
            case Vector2 left when left == Vector2.left:
                if (angle is > 90 and < 180)
                {
                    dashDir = -transform.up;
                }
                else if (angle is > -180 and < -90)
                {
                    dashDir = transform.up;
                }
                break;
        }

        _lastMoveDirection = dashDir;


        _view.RB.linearVelocity = Vector2.zero;
        _view.RB.AddForce(dashDir * _model.DashForce);
        AudioService.Instance.PlaySFXAtPosition(_audio.movementLoop, transform.position);
        _isDashing = true;
        _lastDash = Time.time;
        GameEvents.RaiseDashStarted();
        _view.Animator.SetBool("IsGrounded", false);
        _view.TrailRendererComponent.emitting = true;
        RotateSprites(direction);

        MoveTracker.RegisterMove();
    }

    private bool CheckDashCD()
    {
        var context = PowerUpManager.Instance?.context;
        if (context != null && context.DashTurboActive)
            _model.SetDashCD(_model.DashCD * context.DashCooldownMultiplier);

        return Time.time >= _lastDash + _model.DashCD;
    }

    private void RotateSprites(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _view.SpriteContainer.transform.rotation = Quaternion.Euler(0, 0, angle);
        SetFlipped(angle);
        SetMirrored(false);
    }


    private void TryParry(Vector2 tapPos)
    {
        if (!_isKO && !_isDashing && !_isParrying && CheckParryCD())
        {
            Vector2 dir = CalculateDirection(tapPos);
            Parry(dir);
        }

    }

    private Vector2 CalculateDirection(Vector2 tapPos)
    {
        Vector2 worldTapPos = Camera.main.ScreenToWorldPoint(tapPos);
        Vector2 direction = (worldTapPos - (Vector2)transform.position).normalized;
        return direction;
    }

    private bool CheckParryCD() => Time.time >= _lastParry + _model.ParryCD;

    private void Parry(Vector2 dirToParry)
    {
        if (_isDashing || _isKO)
        {
            return;
        }

        float _parryRange = SetParryRange();
        _isParrying = true;

        _lastParry = Time.time;
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, _parryRange, _proyectilesLayer);

        _view.Animator.SetTrigger("OnParry");
        AudioService.Instance.PlaySFXAtPosition(_audio.parrySwing, transform.position);

        foreach (var col in hitColliders)
        {
            if (col.TryGetComponent(out Projectile projectile)
                && projectile.Shooter != transform
                && projectile.IsParryable)
            {
                projectile.ReflectBackwards(transform, dirToParry);
                HapticFeedback.LightFeedback();
                AudioService.Instance.PlaySFXAtPosition(_audio.projectileParried, transform.position);
                OnParry?.Invoke(false);
                GameEvents.RaiseParrySuccessful();
                break;
            }
        }
        _isParrying = false;
    }

    private float SetParryRange()
    {
        var context = PowerUpManager.Instance?.context;
        if (context != null && context.ParryPerfectActive)
            return _model.ParryRange + context.ParryBonusWindow;
        else return _model.ParryRange;
    }

    private void Grab(Vector2 normal)
    {
        if (_isDashing)
        {
            GameEvents.RaiseDashEnded();
        }
        _isDashing = false;
        _lastNormal = normal;
        _view.Animator.SetBool("IsGrounded", true);
        RotateSprites(Vector2.zero);
        _view.RB.linearVelocity = Vector2.zero;
        _view.Animator.SetBool("IsWallGrabbed", false);
        _view.Animator.SetBool("IsCeilingGrabbed", false);
        _view.LandingParticles.Play();

        if (normal == Vector2.right || normal == Vector2.left)
        {
            _view.Animator.SetBool("IsWallGrabbed", true);
            RotateSprites(Vector2.zero);
            SetMirrored(normal == Vector2.left);
        }
        else if (normal == Vector2.down)
        {
            _view.Animator.SetBool("IsCeilingGrabbed", true);
            RotateSprites(Vector2.left);
        }
    }

    public void Die()
    {
        Debug.Log("Player Died");
        if (_isKO) return;
        _isKO = true;
        OnHit?.Invoke(false);

        _view.TriggerCol.enabled = false;

        _view.RB.bodyType = RigidbodyType2D.Dynamic;
        _view.RB.gravityScale = 1f;

        _view.Animator.SetTrigger("OnKO");

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.TriggerShake(0.4f, 0.5f);
        }

        if (UIManager.Instance.IsHapticFeedbackActive) HapticFeedback.HeavyFeedback();

        GameManager.Instance?.OnPlayerLose();
    }
    #endregion

    #region COLLISION DETECTION
    private void OnCollisionEnter2D(Collision2D collision)
    {
        string colTag = collision.gameObject.tag;
        if (colMatrix.Contains(colTag))
        {
            _view.TrailRendererComponent.emitting = false;
            collision.collider.TryGetComponent(out PlatformBase platform);

            if(_currentPlatform != null)
            {
                _currentPlatform.OnPlayerExit(gameObject, true);
                _currentPlatform = null;
            }
            if(platform == null)
            {
                AudioService.Instance.PlaySFXAtPosition(_audio.landGeneral, transform.position);
                Grab(collision.GetContact(0).normal);
            }
            else if(platform.Type != PlatformTypes.Elastic)
            {
                _currentPlatform = platform;
                Grab(collision.GetContact(0).normal);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        string colTag = collision.gameObject.tag;

        if (deadlyMatrix.Contains(colTag))
        {
            switch (colTag)
            {
                case "Enemy":
                    if (_isDashing)
                    {
                        collision.TryGetComponent(out Enemy enemy);
                        enemy.Die();
                        HapticFeedback.MediumFeedback();
                        AudioService.Instance.PlaySFXAtPosition(_audio.attack, transform.position);
                        PlaySlashVFX(transform.position, _lastMoveDirection);
                    }
                    else
                    {
                        Die();
                    }
                    break;
                case "EnemyShield":
                    _view.RB.linearVelocity = Vector2.zero;
                    Die();
                    break;
                case "Spikes": //TODO: Evitar Die si tiene power up
                    Die();
                    break;
                default:
                    BoxCollider2D[] boxCollider2Ds = GetComponents<BoxCollider2D>();
                    foreach(var col in boxCollider2Ds)
                    {
                        if (col.IsTouching(collision))
                        {
                            Die();
                        }
                    }
                    break;
            }
        }
        else
        {
            if (colTag == "Chain")
            {
                collision.GetComponentInParent<Chain>().DetectCollision();
            }
        }

    }
    #endregion


    #region FOREIGN SYSTEM INTERACTIONS
    public void CalculateAngleRange(Vector2 v)
    {
        Vector2 origin = (Vector2)transform.position;

        Vector2 rayDir = Vector2.zero;
        rayDir.x = -_lastNormal.y;
        rayDir.y = _lastNormal.x;

        float baseAngle = Mathf.Atan2(_lastNormal.y, _lastNormal.x) * Mathf.Rad2Deg;

        RaycastHit2D hit = Physics2D.Raycast(origin, rayDir, .5f, _obstaclesLayer);

        if (hit.collider != null)
        {
            minRange = NormalizeAngle(baseAngle);
            maxRange = NormalizeAngle(baseAngle - 90f);
            return;
        }

        hit = Physics2D.Raycast(origin, -rayDir, .5f, _obstaclesLayer);
        
        if (hit.collider != null)
        {
            minRange = NormalizeAngle(baseAngle); ;
            maxRange = NormalizeAngle(baseAngle + 90f);
            return;
        }

        minRange = NormalizeAngle(baseAngle - 90f);
        maxRange = NormalizeAngle(baseAngle + 90f);
    }
    float NormalizeAngle(float angle)
    {
        angle = (angle + 180f) % 360f;
        if (angle < 0) angle += 360f;
        return angle - 180f;
    }

    private void PlaySlashVFX(Vector2 pos, Vector3 dir)
    {
        Transform newVFX = Instantiate(_slashVFX, pos, Quaternion.identity).transform;
        Quaternion newRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        newVFX.rotation = newRotation;
    }

    #endregion
#if UNITY_EDITOR
    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireSphere(transform.position, _model.ParryRange);
    //}
#endif
}
