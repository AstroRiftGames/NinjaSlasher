using Managers;
using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

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
    private string[] colMatrix = { "Obstacle", "Scenario", "Platform"};


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

    #region INPUT HANDLING
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

    private void Grab()
    {
        _isDashing = false;
        _view.RB.linearVelocity = Vector2.zero;
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
    #endregion
}