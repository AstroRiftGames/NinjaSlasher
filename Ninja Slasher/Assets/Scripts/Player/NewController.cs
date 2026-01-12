using Managers;
using System;
using UnityEngine;
using CandyCoded.HapticFeedback;
using System.Linq;
using Unity.VisualScripting;
using System.Collections;

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

    private bool _isKO = false;
    private bool _isDashing = false;
    private float _lastParry;
    private float _lastDash;
    [SerializeField] private LayerMask _proyectilesLayer;

    private string[] colMatrix = { "Obstacle", "Scenario", };
    private string[] deadlyMatrix = { "Enemy", "Projectile", "Spikes", "EnemyShield", };

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
    private void Awake()
    {
        //InitializeFSM();
        //InitializeTree(); 
        _swipeDetection.OnSwipe += context => { TryDash(context); };
        _swipeDetection.OnTap += context => { TryParry(context); } ;
    }
    #endregion

    #region MECHANICS
    private void TryDash(Vector2 direction)
    {
        if(!_isKO && !_isDashing && CheckDashCD())
        {
            Dash(direction);
        }
    }
    private void Dash(Vector2 direction)
    {
        _view.RB.AddForce(direction * _model.DashForce);
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.P_Movement, transform.position);
        _isDashing = true;
        _lastDash = Time.time;
        _view.Animator.SetBool("IsGrounded", false);
        RotateSprites(direction);

        MoveTracker.RegisterMove();
        NotifyTutorialDashPerformed();
        NotifyTutorialParryPerformed();
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
        if(!_isKO && !_isDashing && CanParry())
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

    private bool CanParry() => Time.time >= _lastParry + _model.ParryCD;

    private void Parry(Vector2 dirToParry)
    {
        if (_isDashing || _isKO)
        {
            return;
        }

        float _parryRange = SetParryRange();

        _lastParry = Time.time;
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, _parryRange, _proyectilesLayer);

        _view.Animator.SetTrigger("OnParry");
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.P_ParrySwing, transform.position);

        foreach (var col in hitColliders)
        {
            if (col.TryGetComponent(out Projectile projectile)
                && projectile.Shooter != transform
                && projectile.IsParryable)
            {
                projectile.ReflectBackwards(transform, dirToParry);
                HapticFeedback.LightFeedback();
                AudioManager.Instance.PlaySFXAtPosition(SFXClip.P_ProjectileParried, transform.position);

                return;
            }
        }
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
        _isDashing = false;
        _view.Animator.SetBool("IsGrounded", true);
        RotateSprites(Vector2.zero);
        _view.RB.linearVelocity = Vector2.zero;
        _view.Animator.SetBool("IsWallGrabbed", false);
        _view.Animator.SetBool("IsCeilingGrabbed", false);
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.P_Landing_General, transform.position);

        if (normal == Vector2.right || normal == Vector2.left)
        {
            _view.Animator.SetBool("IsWallGrabbed", true);
            RotateSprites(Vector2.zero);
            SetMirrored(normal == Vector2.left);
        }
        else if(normal == Vector2.down)
        {
            _view.Animator.SetBool("IsCeilingGrabbed", true);
            RotateSprites(Vector2.left);
        }
    }

    public void Die()
    {
        Debug.Log("Player Died");
        if(_isKO) return;
        _isKO = true;

        _view.TriggerCol.enabled = false;

        _view.RB.bodyType = RigidbodyType2D.Dynamic;
        _view.RB.gravityScale = 1f;

        _view.Animator.SetTrigger("OnKO");

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.TriggerShake(0.4f, 0.5f);
        }

        var levelController = FindObjectOfType<LevelController>();
        if (levelController != null)
        {
            levelController.MarkLevelAsFailed();
        }

        LevelManager.Instance.OnPlayerLose();
        if (UIManager.Instance.IsHapticFeedbackActive) HapticFeedback.HeavyFeedback();
    }
    #endregion

    #region COLLISION DETECTION
    private void OnCollisionEnter2D(Collision2D collision)
    {
        string colTag = collision.gameObject.tag;
        if (colMatrix.Contains(colTag))
        {

            collision.collider.TryGetComponent(out ElasticPlatform elasticComponent);

            if (!elasticComponent)
            {
                Grab(collision.GetContact(0).normal);
            }
            else
            {
                //TODO: Bounce on elastic platform
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        string colTag = collision.gameObject.tag;
        
        Debug.Log($"Collided with: {colTag} ({collision.name})");
        if(deadlyMatrix.Contains(colTag))
        {
            switch (colTag)
            {
                case "Enemy":
                    if (_isDashing) 
                    {
                        collision.TryGetComponent(out Enemy enemy);
                        enemy.Die();
                        HapticFeedback.MediumFeedback();
                        AudioManager.Instance.PlaySFXAtPosition(SFXClip.P_Attack, transform.position);
                        StartCoroutine(SlashEffectCoroutine());
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
                    Die();
                    break;
            }
        }

    }
    #endregion


    #region FOREIGN SYSTEM INTERACTIONS

    private void NotifyTutorialDashPerformed()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnDashPerformed();
        }
    }

    private void NotifyTutorialParryPerformed()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnParryPerformed();
        }
    }
    private IEnumerator SlashEffectCoroutine()
    {
        TrailRenderer slashTrail = _view.SlashTrail;

        if (slashTrail != null)
        {
            slashTrail.Clear();
            slashTrail.emitting = true;

            yield return new WaitForSeconds(_model.SlashEffectDuration);

            slashTrail.emitting = false;
        }
    }

    #endregion
#if UNIT_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _model.ParryRange);
    }
#endif
}