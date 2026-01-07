using Managers;
using System;
using UnityEngine;
using CandyCoded.HapticFeedback;
using System.Linq;
using Unity.VisualScripting;

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

    private bool _isKO = false;
    private bool _isDashing = false;
    private bool _isParrying = false;
    private string[] colMatrix = { "Obstacle", "Scenario", "Platform", };
    private string[] deadlyMatrix = { "Enemy", "Projectile", "Spikes", "EnemyShield", };


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
        SwipeDetection.instance.OnSwipe += context => { TryDash(context); };
        SwipeDetection.instance.OnTap += TryParry;
    }
    void OnEnable()
    {
        CustomUpdateManager.Instance.SubscribeToUpdate(CustomUpdate);
    }

    private void OnDisable()
    {
        CustomUpdateManager.Instance.UnsubscribeFromUpdate(CustomUpdate);
    }

    private void CustomUpdate()
    {

        if (Input.GetKeyDown(KeyCode.F)) Die();
        if(_isKO)
        {
            Debug.Log("Player is KO'd");
        }
        else
        {
            Debug.Log("Player is active");
        }
    }
    #endregion

    #region MECHANICS
    private void TryDash(Vector2 direction)
    {
        if(!_isKO && !_isDashing)
        {
            Dash(direction);
        }
    }
    private void Dash(Vector2 direction)
    {
        Debug.DrawRay(transform.position, direction, Color.red, 2f);
        _view.RB.AddForce(direction * _model.DashForce);
        _isDashing = true;
    }

    private void TryParry()
    {
        if(!_isKO && !_isParrying && !_isDashing)
        {
            Parry();
        }
        
    }

    private void Parry()
    {
        Debug.Log("Parry");
    }

    private void Grab()
    {
        _isDashing = false;
        _view.RB.linearVelocity = Vector2.zero;
    }

    public void Die()
    {
        Debug.Log("Player Died");
        if(_isKO) return;
        _isKO = true;

        _view.TriggerCol.enabled = false;

        _view.RB.bodyType = RigidbodyType2D.Dynamic;
        _view.RB.gravityScale = 1f;

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
            Grab();
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
}