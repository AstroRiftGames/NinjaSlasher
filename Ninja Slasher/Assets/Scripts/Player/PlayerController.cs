using CandyCoded.HapticFeedback;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public enum NinjaStates
{
    Idle,
    Dash,
    Grab,
    KO
}

public class PlayerController : MonoBehaviour
{
    public PlayerView View => _view;
    [SerializeField] PlayerView _view;
    public PlayerModel Model => _model;
    [SerializeField] PlayerModel _model;

    [SerializeField] InputDetection _swipeDetection;
    [SerializeField] TrajectoryRenderer _trajectoryRenderer;
    [SerializeField] private GameObject _slashVFX;

    [Header("Audio")]
    [SerializeField] private PlayerAudioSet _audio;

    [Header("Smoke Bomb Settings")]
    [SerializeField] private float _smokeBombVisibilityDelay = 0.15f;


    public bool IsDashing => _isDashing;
    private bool _isDashing = false;
    public bool IsParrying => _isParrying;
    private bool _isParrying = false;
    public bool IsDeadOrDying => _isKO;
    private bool _isKO = false;
    private bool _hasHandledGameplayClosed = false;

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

        UIEvents.OnTransitionFinished += PlaySmokeBomb;
    }

    private void OnDisable()
    {
        if (_swipeDetection == null) return;

        _swipeDetection.OnSwipe -= TryDash;
        _swipeDetection.OnInputStart -= CalculateAngleRange;
        _swipeDetection.OnTap -= TryParry;

        UIEvents.OnTransitionFinished -= PlaySmokeBomb;
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
        if (LevelSessionManager.Instance != null && !LevelSessionManager.Instance.CanProcessGameplay)
        {
            HandleGameplayClosed();
            _trajectoryRenderer.HideTrajectory();
            return;
        }

        _hasHandledGameplayClosed = false;

        if (_swipeDetection.IsPressing && _swipeDetection.Direction.magnitude >= .5f)
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
        if (LevelSessionManager.Instance != null && !LevelSessionManager.Instance.CanProcessGameplay)
            return;

        if (!_isKO)
        {
            Dash(direction);
        }
    }
    private void TryDash(Vector2 direction)
    {
        if (LevelSessionManager.Instance != null && !LevelSessionManager.Instance.CanProcessGameplay)
            return;

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
    
    private void PlaySmokeBomb()
    {
        if (_view.SmokeBombParticles != null)
        {
            _view.SmokeBombParticles.Play();
            StartCoroutine(ShowCharacterAfterDelay(_smokeBombVisibilityDelay));
        }
        else
        {
            _view.SetSpriteVisibility(true);
        }
    }

    private IEnumerator ShowCharacterAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _view.SetSpriteVisibility(true);
    }


    private void TryParry(Vector2 tapPos)
    {
        if (LevelSessionManager.Instance != null && !LevelSessionManager.Instance.CanProcessGameplay)
            return;

        if (!_isKO && !_isDashing && !_isParrying && CheckParryCD())
        {
            Vector2 worldTapPos = Camera.main.ScreenToWorldPoint(tapPos);
            Parry(worldTapPos);
        }
    }

    private bool CheckParryCD() => Time.time >= _lastParry + _model.ParryCD;

    private void Parry(Vector2 worldTapPos)
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
                Vector2 dirToParry = (worldTapPos - (Vector2)projectile.transform.position).normalized;
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

    private void Grab(Vector2 normal, Vector2 contactPoint, Collider2D surfaceCollider)
    {
        bool wasDashing = _isDashing;
        if (wasDashing)
            _isDashing = false;

        _lastNormal = normal;
        _view.Animator.SetBool("IsGrounded", true);
        RotateSprites(Vector2.zero);
        _view.RB.linearVelocity = Vector2.zero;
        _view.Animator.SetBool("IsWallGrabbed", false);
        _view.Animator.SetBool("IsCeilingGrabbed", false);

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
        
        // Reposition and align particles AFTER all rotations and mirroring
        _view.LandingParticles.transform.position = contactPoint;
        _view.LandingParticles.transform.up = normal;
        
        SurfaceMaterial material = SurfaceMaterial.General;
        if (surfaceCollider != null && surfaceCollider.TryGetComponent(out SurfaceProperties surfaceProps))
        {
            material = surfaceProps.MaterialType;
        }
        
        _view.SetLandingParticlesSurface(material);
        _view.LandingParticles.Play();

        if (wasDashing)
        {
            GameEvents.RaiseDashEnded();
        }
    }

    public void Die()
    {
        if (_isKO) return;

        if(_currentPlatform != null)
        {
            _currentPlatform.OnPlayerExit(gameObject, true);
            _currentPlatform = null;
        }
        PlayKnockOutSfx();

        _isKO = true;
        OnHit?.Invoke(false);

        _view.TriggerCol.enabled = false;

        _view.RB.bodyType = RigidbodyType2D.Dynamic;
        _view.RB.gravityScale = 1f;

        RotateSprites(Vector2.zero);
        _view.Animator.SetTrigger("OnKO");

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.TriggerShake(0.4f, 0.5f);
        }

        if (UIManager.Instance.IsHapticFeedbackActive)
        {
            HapticFeedback.HeavyFeedback();
        }

        GameManager.Instance?.OnPlayerLose();
    }

    private void PlayKnockOutSfx()
    {
        if (_audio == null)
        {
            return;
        }

        AudioEvent selectedEvent = GetRandomKnockOutEvent();
        if (selectedEvent == null)
        {
            return;
        }

        AudioService.Instance.PlaySFXAtPosition(selectedEvent, transform.position);
    }

    private AudioEvent GetRandomKnockOutEvent()
    {
        if (_audio.koVariants != null && _audio.koVariants.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, _audio.koVariants.Length);
            return _audio.koVariants[randomIndex];
        }

        return _audio.die;
    }
    #endregion

    #region COLLISION DETECTION
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!CanResolveActionOnSurface())
            return;

        string colTag = collision.gameObject.tag;
        if (colMatrix.Contains(colTag))
        {
            var lastContact = collision.contacts.Last();
            ProcessSurfaceCollision(collision.collider, lastContact.normal, lastContact.point);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!CanResolveActionOnSurface())
            return;

        if (!_isDashing) return;

        string colTag = collision.gameObject.tag;
        if (colMatrix.Contains(colTag))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (Vector2.Dot(contact.normal, _lastMoveDirection) < -0.5f)
                {
                    ProcessSurfaceCollision(collision.collider, contact.normal, contact.point);
                    break;
                }
            }
        }
    }

    private Vector2 GetCardinalNormal(Vector2 rawNormal)
    {
        if (Mathf.Abs(rawNormal.x) > Mathf.Abs(rawNormal.y))
        {
            return rawNormal.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            return rawNormal.y > 0 ? Vector2.up : Vector2.down;
        }
    }

    private void ProcessSurfaceCollision(Collider2D col, Vector2 normal, Vector2 contactPoint)
    {
        Vector2 cleanNormal = GetCardinalNormal(normal);

        _view.TrailRendererComponent.emitting = false;
        col.TryGetComponent(out PlatformBase platform);

        if (_currentPlatform != null)
        {
            _currentPlatform.OnPlayerExit(gameObject, true);
            _currentPlatform = null;
        }

        if (platform == null)
        {
            AudioService.Instance.PlaySFXAtPosition(_audio.landGeneral, transform.position);
            Grab(cleanNormal, contactPoint, col);
        }
        else if (platform.Type != PlatformTypes.Elastic)
        {
            _currentPlatform = platform;
            Grab(cleanNormal, contactPoint, col);
        }
    }

    private bool IsActionInProgress()
    {
        if (_isKO)
            return false;

        return _isDashing;
    }

    private bool CanResolveActionOnSurface()
    {
        if (LevelSessionManager.Instance == null || LevelSessionManager.Instance.CanProcessGameplay)
            return !_isKO;

        return IsActionInProgress();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (LevelSessionManager.Instance != null && !LevelSessionManager.Instance.CanProcessGameplay)
            return;

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
        if (LevelSessionManager.Instance != null && !LevelSessionManager.Instance.CanProcessGameplay)
            return;

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

    private void HandleGameplayClosed()
    {
        if (_hasHandledGameplayClosed)
            return;

        _hasHandledGameplayClosed = true;
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
